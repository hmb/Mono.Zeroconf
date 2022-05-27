//
// ServiceBrowser.cs
//
// Author:
//    Holger Böhnke     <zeroconf@biz.amarin.de>
//
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

public class ServiceTypeBrowser : IServiceTypeBrowser
{
    private class CountedBrowser
    {
        public CountedBrowser(ServiceBrowser serviceBrowser)
        {
            this.ServiceBrowser = serviceBrowser;
        }

        public readonly ServiceBrowser ServiceBrowser;
        public int UsageCount = 1;
    }

    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger logger;
    private readonly Dictionary<string, CountedBrowser> serviceBrowsers = new();
    private readonly AsyncLock serviceTypeBrowserLock;
    private readonly AsyncLock serviceBrowsersLock;

    private readonly int interfaceIndex;
    private readonly IpProtocolType ipProtocolType;

    private DBus.IServiceTypeBrowser? serviceTypeBrowser;
    private IDisposable? newServiceTypeWatcher;
    private IDisposable? removeServiceTypeWatcher;

    public ServiceTypeBrowser(
        ILoggerFactory loggerFactory,
        int interfaceIndex,
        IpProtocolType ipProtocolType,
        string replyDomain)
    {
        this.loggerFactory = loggerFactory;
        this.logger = loggerFactory.CreateLogger<ServiceTypeBrowser>();
        this.serviceTypeBrowserLock = new AsyncLock(this.logger);
        this.serviceBrowsersLock = new AsyncLock(this.logger);
        this.interfaceIndex = interfaceIndex;
        this.ipProtocolType = ipProtocolType;
        this.ReplyDomain = replyDomain;
    }

    public void Dispose()
    {
        this.StopBrowse().GetAwaiter().GetResult();
    }

    public event EventHandler<ServiceTypeBrowseEventArgs>? ServiceTypeAdded;
    public event EventHandler<ServiceTypeBrowseEventArgs>? ServiceTypeRemoved;

    public uint InterfaceIndex => AvahiUtils.AvahiToZeroconfInterfaceIndex(this.interfaceIndex);

    public Abstraction.IpProtocolType IpProtocolType => AvahiUtils.AvahiToZeroconfIpProtocolType(this.ipProtocolType);

    public string ReplyDomain { get; }


    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    public IEnumerator<IServiceBrowser> GetEnumerator()
    {
        using (this.serviceBrowsersLock.Enter("GetEnumerator").GetAwaiter().GetResult())
        {
            return this.serviceBrowsers.Values.Select(st => st.ServiceBrowser).ToList().GetEnumerator();
        }
    }

    public async Task Browse()
    {
        using (await this.serviceTypeBrowserLock.Enter("Browse"))
        {
            if (DBusManager.Server == null)
            {
                throw new ConnectException("no connection to the avahi daemon possible");
            }

            if (this.serviceTypeBrowser != null)
            {
                throw new InvalidOperationException("The service type browser is already running");
            }

            this.serviceTypeBrowser = await DBusManager.Server.ServiceTypeBrowserNewAsync(
                this.interfaceIndex,
                (int)this.ipProtocolType,
                this.ReplyDomain,
                (uint)LookupFlags.None);

            this.newServiceTypeWatcher = await this.serviceTypeBrowser.WatchItemNewAsync(this.OnServiceTypeNew);
            this.removeServiceTypeWatcher =
                await this.serviceTypeBrowser.WatchItemRemoveAsync(this.OnServiceTypeRemove);
        }
    }

    public async Task StopBrowse()
    {
        using (this.serviceTypeBrowserLock.Enter("Dispose").GetAwaiter().GetResult())
        {
            this.newServiceTypeWatcher?.Dispose();
            this.newServiceTypeWatcher = null;

            this.removeServiceTypeWatcher?.Dispose();
            this.removeServiceTypeWatcher = null;

            if (this.serviceTypeBrowser != null)
            {
                await this.serviceTypeBrowser.FreeAsync();
                this.serviceTypeBrowser = null;
            }

            await this.ClearServiceBrowsers();
        }
    }

    private async Task ClearServiceBrowsers()
    {
        using (await this.serviceBrowsersLock.Enter("ClearServiceBrowsers"))
        {
            foreach (var service in this.serviceBrowsers.Values)
            {
                this.RaiseServiceTypeRemoved(service.ServiceBrowser);
                service.ServiceBrowser.Dispose();
            }

            this.serviceBrowsers.Clear();
        }
    }

    private void RaiseServiceTypeAdded(IServiceBrowser service)
    {
        this.ServiceTypeAdded?.Invoke(this, new ServiceTypeBrowseEventArgs(service));
    }

    private void RaiseServiceTypeRemoved(IServiceBrowser service)
    {
        this.ServiceTypeRemoved?.Invoke(this, new ServiceTypeBrowseEventArgs(service));
    }

    private async void OnServiceTypeNew(
        (int interfaceIndex, int ipProtocolType, string regtype, string domain, uint flags) serviceType)
    {
        using (await this.serviceBrowsersLock.Enter("OnServiceTypeNew"))
        {
            var key = GetServiceNameKey(
                serviceType.interfaceIndex,
                serviceType.ipProtocolType,
                serviceType.regtype,
                serviceType.domain);

            if (this.serviceBrowsers.TryGetValue(key, out var existingServiceType))
            {
                this.logger.LogDebug("service browser {Key} was already added, increment usage", key);
                ++existingServiceType.UsageCount;
            }
            else
            {
                this.logger.LogDebug("create new service browser {Key}", key);
                var newServiceBrowser = new ServiceBrowser(
                    this.loggerFactory,
                    serviceType.interfaceIndex,
                    (IpProtocolType)serviceType.ipProtocolType,
                    serviceType.regtype,
                    serviceType.domain);

                this.serviceBrowsers.Add(key, new CountedBrowser(newServiceBrowser));

                this.RaiseServiceTypeAdded(newServiceBrowser);
            }
        }
    }

    private async void OnServiceTypeRemove(
        (int interfaceIndex, int ipProtocolType, string regtype, string domain, uint flags) serviceType)
    {
        using (await this.serviceBrowsersLock.Enter("OnServiceTypeRemove"))
        {
            var key = GetServiceNameKey(
                serviceType.interfaceIndex,
                serviceType.ipProtocolType,
                serviceType.regtype,
                serviceType.domain);

            if (!this.serviceBrowsers.TryGetValue(key, out var existingServiceType))
            {
                this.logger.LogWarning("resolver was never added: {Key}", key);
                return;
            }

            this.logger.LogDebug(
                "decrement usage count {Count} on resolver {Key}",
                existingServiceType.UsageCount,
                key);
            
            --existingServiceType.UsageCount;
            if (existingServiceType.UsageCount > 0)
            {
                return;
            }

            this.logger.LogDebug("usage count on resolver {Key} is down to zero, remove it", key);
            this.serviceBrowsers.Remove(key);
            this.RaiseServiceTypeRemoved(existingServiceType.ServiceBrowser);
            await existingServiceType.ServiceBrowser.StopBrowse();
        }
    }

    private static string GetServiceNameKey(int interfaceIndex, int protocol, string type, string domain)
    {
        return $"{interfaceIndex}_{protocol}_{type}_{domain}";
    }
}