//
// ServiceBrowser.cs
//
// Author:
//    Aaron Bockover    <abockover@novell.com>
//    Holger Böhnke     <zeroconf@biz.amarin.de>
//
// Copyright (C) 2007-2008 Novell, Inc.
// Copyright (C) 2022 Holger Böhnke, (http://www.amarin.de)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Zeroconf.Avahi;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tmds.DBus;
using Zeroconf.Abstraction;
using Zeroconf.Avahi.Threading;

public class ServiceBrowser : IServiceBrowser
{
    private class CountedResolver
    {
        public CountedResolver(ServiceResolver serviceResolver)
        {
            this.ServiceResolver = serviceResolver;
        }

        public readonly ServiceResolver ServiceResolver;
        public int UsageCount = 1;
    }

    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger logger;
    private readonly Dictionary<string, CountedResolver> serviceResolvers = new();
    private readonly AsyncLock serviceBrowserLock;
    private readonly AsyncLock serviceResolverLock;
    
    private readonly int interfaceIndex;
    private readonly IpProtocolType ipProtocolType;

    private DBus.IServiceBrowser? serviceBrowser;
    private IDisposable? newServiceWatcher;
    private IDisposable? removeServiceWatcher;

    public ServiceBrowser(ILoggerFactory loggerFactory, int interfaceIndex, IpProtocolType ipProtocolType, string regType, string replyDomain)
    {
        this.loggerFactory = loggerFactory;
        this.logger = loggerFactory.CreateLogger<ServiceBrowser>();
        this.serviceBrowserLock = new AsyncLock(this.logger);
        this.serviceResolverLock = new AsyncLock(this.logger);
        this.interfaceIndex = interfaceIndex;
        this.ipProtocolType = ipProtocolType;
        this.RegType = regType;
        this.ReplyDomain = replyDomain;
    }
    
    public void Dispose()
    {
        this.StopBrowse().GetAwaiter().GetResult();
    }

    public event EventHandler<ServiceBrowseEventArgs>? ServiceAdded;
    public event EventHandler<ServiceBrowseEventArgs>? ServiceRemoved;

    public uint InterfaceIndex => AvahiUtils.AvahiToZeroconfInterfaceIndex(this.interfaceIndex);

    public Abstraction.IpProtocolType IpProtocolType => AvahiUtils.AvahiToZeroconfIpProtocolType(this.ipProtocolType);

    public string RegType { get; }
    public string ReplyDomain { get; }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    public IEnumerator<IResolvableService> GetEnumerator()
    {
        using (this.serviceResolverLock.Enter("GetEnumerator").GetAwaiter().GetResult())
        {
            return this.serviceResolvers.Values.Select(sr => sr.ServiceResolver).ToList().GetEnumerator();
        }
    }

    public async Task Browse()
    {
        using (await this.serviceBrowserLock.Enter("Browse"))
        {
            if (DBusManager.Server == null)
            {
                throw new ConnectException("no connection to the avahi daemon possible");
            }

            if (this.serviceBrowser != null)
            {
                throw new InvalidOperationException("The service browser is already running");
            }

            this.logger.LogDebug("browse: ServiceBrowserNewAsync");
            this.serviceBrowser = await DBusManager.Server.ServiceBrowserNewAsync(
                this.interfaceIndex,
                (int)this.ipProtocolType,
                this.RegType,
                this.ReplyDomain,
                (uint)LookupFlags.None);

            this.logger.LogDebug("browse: WatchItemNewAsync");
            this.newServiceWatcher = await this.serviceBrowser.WatchItemNewAsync(this.OnServiceNew);
            this.logger.LogDebug("browse: WatchItemRemoveAsync");
            this.removeServiceWatcher = await this.serviceBrowser.WatchItemRemoveAsync(this.OnServiceRemove);
        }
    }

    public async Task StopBrowse()
    {
        using (await this.serviceBrowserLock.Enter("StopBrowse"))
        {
            this.newServiceWatcher?.Dispose();
            this.newServiceWatcher = null;

            this.removeServiceWatcher?.Dispose();
            this.removeServiceWatcher = null;

            if (this.serviceBrowser != null)
            {
                await this.serviceBrowser.FreeAsync();
                this.serviceBrowser = null;
            }
            
            await this.ClearResolvers();
        }
    }

    private async Task ClearResolvers()
    {
        using (await this.serviceResolverLock.Enter("Clear resolvers"))
        {
            foreach (var service in this.serviceResolvers.Values)
            {
                this.RaiseServiceRemoved(service.ServiceResolver);
                await service.ServiceResolver.StopResolve();
            }

            this.serviceResolvers.Clear();
        }
    }

    private void RaiseServiceAdded(IResolvableService service)
    {
        this.ServiceAdded?.Invoke(this, new ServiceBrowseEventArgs(service));
    }

    private void RaiseServiceRemoved(IResolvableService service)
    {
        this.ServiceRemoved?.Invoke(this, new ServiceBrowseEventArgs(service));
    }

    private async void OnServiceNew((int interfaceIndex, int protocol, string name, string regtype, string domain, uint flags) serviceData)
    {
        using (await this.serviceResolverLock.Enter("OnServiceNew"))
        {
            var key = GetServiceNameKey(
                serviceData.interfaceIndex,
                serviceData.protocol,
                serviceData.name,
                serviceData.regtype,
                serviceData.domain);

            if (this.serviceResolvers.TryGetValue(key, out var existingServiceResolver))
            {
                this.logger.LogDebug($"resolver {key} was already added, increment usage");
                ++existingServiceResolver.UsageCount;
            }
            else
            {
                this.logger.LogDebug($"create new resolver {key}");
                var newServiceResolver = new ServiceResolver(
                    this.loggerFactory,
                    serviceData.interfaceIndex,
                    (IpProtocolType)serviceData.protocol,
                    serviceData.name,
                    serviceData.regtype,
                    serviceData.domain);

                this.serviceResolvers.Add(key, new CountedResolver(newServiceResolver));
                this.RaiseServiceAdded(newServiceResolver);
            }
        }
    }

    private async void OnServiceRemove((int interfaceIndex, int protocol, string name, string regtype, string domain, uint flags) serviceData)
    {
        using (await this.serviceResolverLock.Enter("OnServiceRemove"))
        {
            var key = GetServiceNameKey(
                serviceData.interfaceIndex,
                serviceData.protocol,
                serviceData.name,
                serviceData.regtype,
                serviceData.domain);

            if (!this.serviceResolvers.TryGetValue(key, out var resolverInstance))
            {
                this.logger.LogDebug($"ERROR: resolver was never added: {key}");
                return;
            }

            this.logger.LogDebug($"decrement usage count {resolverInstance.UsageCount} on resolver {key}");
            --resolverInstance.UsageCount;

            if (resolverInstance.UsageCount > 0)
            {
                return;
            }

            this.logger.LogDebug($"usage count on resolver {key} down to zero, remove it");
            this.serviceResolvers.Remove(key);
            this.RaiseServiceRemoved(resolverInstance.ServiceResolver);
            await resolverInstance.ServiceResolver.StopResolve();
        }
    }

    private static string GetServiceNameKey(int interfaceIndex, int protocol, string name, string type, string domain)
    {
        return $"{interfaceIndex}_{protocol}_{name}_{type}_{domain}";
    }
}