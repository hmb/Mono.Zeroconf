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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Mono.Zeroconf.Providers.Avahi.Threading;
using Tmds.DBus;

namespace Mono.Zeroconf.Providers.Avahi;

public class ServiceBrowser : IServiceBrowser
{
    private class CountedBrowseService
    {
        public readonly BrowseService BrowseService;
        public int UsageCount = 1;

        public CountedBrowseService(BrowseService browseService)
        {
            this.BrowseService = browseService;
        }
    }

    private readonly Dictionary<string, CountedBrowseService> services = new();
    private readonly AsyncLock serviceLock = new();

    private DBus.IServiceBrowser? serviceBrowser;
    private IDisposable? newServiceWatcher;
    private IDisposable? removeServiceWatcher;

    public void Dispose()
    {
        using (this.serviceLock.Enter().GetAwaiter().GetResult())
        {
            this.ClearUnSynchronized().GetAwaiter().GetResult();
        }
    }

    public event EventHandler<ServiceBrowseEventArgs>? ServiceAdded;
    public event EventHandler<ServiceBrowseEventArgs>? ServiceRemoved;

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    public IEnumerator<IResolvableService> GetEnumerator()
    {
        using (this.serviceLock.Enter().GetAwaiter().GetResult())
        {
            return this.services.Values.Select(bs => bs.BrowseService).ToList().GetEnumerator();
        }
    }

    public async Task Browse(uint interfaceIndex, AddressProtocol addressProtocol, string? regtype, string? domain)
    {
        using (await this.serviceLock.Enter())
        {
            if (DBusManager.Server == null)
            {
                throw new ConnectException("no connection to the avahi daemon possible");
            }

            await this.ClearUnSynchronized();

            this.serviceBrowser = await DBusManager.Server.ServiceBrowserNewAsync(
                AvahiUtils.FromMzcInterface(interfaceIndex),
                (int)AvahiUtils.FromMzcProtocol(addressProtocol),
                regtype ?? string.Empty,
                domain ?? string.Empty,
                (uint)LookupFlags.None);

            this.newServiceWatcher = await this.serviceBrowser.WatchItemNewAsync(this.OnServiceNew);
            this.removeServiceWatcher = await this.serviceBrowser.WatchItemRemoveAsync(this.OnServiceRemove);
        }
    }

    public async Task Clear()
    {
        using (await this.serviceLock.Enter())
        {
            await this.ClearUnSynchronized();
        }
    }

    private async Task ClearUnSynchronized()
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

        foreach (var service in this.services.Values)
        {
            service.BrowseService.Dispose();
        }

        this.services.Clear();
    }

    private void RaiseServiceAdded(IResolvableService service)
    {
        this.ServiceAdded?.Invoke(this, new ServiceBrowseEventArgs(service));
    }

    private void RaiseServiceRemoved(IResolvableService service)
    {
        this.ServiceRemoved?.Invoke(this, new ServiceBrowseEventArgs(service));
    }

    private async void OnServiceNew(
        (int @interface, int protocol, string name, string regtype, string domain, uint flags) serviceData)
    {
        using (await this.serviceLock.Enter())
        {
            var key = GetServiceNameKey(
                serviceData.@interface,
                serviceData.protocol,
                serviceData.regtype,
                serviceData.name);

            if (this.services.TryGetValue(key, out var existingResolverInstance))
            {
                ++existingResolverInstance.UsageCount;
            }
            else
            {
                var newBrowseService = new BrowseService(
                    serviceData.name,
                    serviceData.regtype,
                    serviceData.domain,
                    serviceData.@interface,
                    (Protocol)serviceData.protocol);

                this.services.Add(key, new CountedBrowseService(newBrowseService));
                this.RaiseServiceAdded(newBrowseService);
            }
        }
    }

    private async void OnServiceRemove(
        (int @interface, int protocol, string name, string regtype, string domain, uint flags) serviceData)
    {
        using (await this.serviceLock.Enter())
        {
            var key = GetServiceNameKey(
                serviceData.@interface,
                serviceData.protocol,
                serviceData.regtype,
                serviceData.name);

            if (!this.services.TryGetValue(key, out var resolverInstance))
            {
                Console.WriteLine($"ERROR: resolver was never added: {key}");
                return;
            }

            Console.WriteLine($"decrement usage count {resolverInstance.UsageCount} of the resolver: {key}");
            --resolverInstance.UsageCount;

            if (resolverInstance.UsageCount > 0)
            {
                return;
            }

            this.RaiseServiceRemoved(resolverInstance.BrowseService);

            await resolverInstance.BrowseService.StopResolve();

            this.services.Remove(key);
        }
    }

    private static string GetServiceNameKey(int networkInterface, int protocol, string type, string name)
    {
        return $"{networkInterface}_{protocol}_{type}_{name}";
    }
}