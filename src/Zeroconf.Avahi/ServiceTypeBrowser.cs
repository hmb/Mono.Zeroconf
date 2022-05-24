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
using Tmds.DBus;
using Zeroconf.Abstraction;
using Zeroconf.Avahi.Threading;

public class ServiceTypeBrowser : IServiceTypeBrowser
{
    private class CountedServiceType
    {
        public CountedServiceType(ServiceType serviceType)
        {
            this.ServiceType = serviceType;
        }

        public readonly ServiceType ServiceType;
        public int UsageCount = 1;
    }

    private readonly Dictionary<string, CountedServiceType> serviceTypes = new();
    private readonly AsyncLock serviceTypeLock = new();

    private DBus.IServiceTypeBrowser? serviceTypeBrowser;
    private IDisposable? newServiceTypeWatcher;
    private IDisposable? removeServiceTypeWatcher;

    public void Dispose()
    {
        using (this.serviceTypeLock.Enter().GetAwaiter().GetResult())
        {
            this.ClearUnSynchronized().GetAwaiter().GetResult();
        }
    }

    public event EventHandler<ServiceTypeBrowseEventArgs>? ServiceTypeAdded;
    public event EventHandler<ServiceTypeBrowseEventArgs>? ServiceTypeRemoved;

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    public IEnumerator<IServiceType> GetEnumerator()
    {
        using (this.serviceTypeLock.Enter().GetAwaiter().GetResult())
        {
            return this.serviceTypes.Values.Select(st => st.ServiceType).ToList().GetEnumerator();
        }
    }

    public async Task Browse(uint interfaceIndex, Abstraction.IpProtocolType ipProtocolType, string? domain)
    {
        using (await this.serviceTypeLock.Enter())
        {
            if (DBusManager.Server == null)
            {
                throw new ConnectException("no connection to the avahi daemon possible");
            }

            await this.ClearUnSynchronized();

            this.serviceTypeBrowser = await DBusManager.Server.ServiceTypeBrowserNewAsync(
                AvahiUtils.ZeroconfToAvahiInterfaceIndex(interfaceIndex),
                (int)AvahiUtils.ZeroconfToAvahiIpAddressProtocol(ipProtocolType),
                domain ?? string.Empty,
                (uint)LookupFlags.None);

            this.newServiceTypeWatcher = await this.serviceTypeBrowser.WatchItemNewAsync(this.OnServiceTypeNew);
            this.removeServiceTypeWatcher = await this.serviceTypeBrowser.WatchItemRemoveAsync(this.OnServiceTypeRemove);
        }
    }

    private async Task ClearUnSynchronized()
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

        foreach (var service in this.serviceTypes.Values)
        {
            service.ServiceType.Dispose();
        }

        this.serviceTypes.Clear();
    }

    private void RaiseServiceTypeAdded(IServiceType service)
    {
        this.ServiceTypeAdded?.Invoke(this, new ServiceTypeBrowseEventArgs(service));
    }

    private void RaiseServiceTypeRemoved(IServiceType service)
    {
        this.ServiceTypeRemoved?.Invoke(this, new ServiceTypeBrowseEventArgs(service));
    }

    private async void OnServiceTypeNew((int interfaceIndex, int ipProtocolType, string regtype, string domain, uint flags) serviceType)
    {
        using (await this.serviceTypeLock.Enter())
        {
            var key = GetServiceNameKey(
                serviceType.interfaceIndex,
                serviceType.ipProtocolType,
                serviceType.regtype);

            if (this.serviceTypes.TryGetValue(key, out var existingServiceType))
            {
                Console.WriteLine($"resolver {key} was already added, increment usage");
                ++existingServiceType.UsageCount;
            }
            else
            {
                Console.WriteLine($"create new resolver {key}");
                var newServiceType = new ServiceType(
                    serviceType.interfaceIndex,
                    (IpProtocolType)serviceType.ipProtocolType,
                    serviceType.regtype,
                    serviceType.domain);

                this.serviceTypes.Add(key, new CountedServiceType(newServiceType));

                this.RaiseServiceTypeAdded(newServiceType);
            }
        }
    }

    private async void OnServiceTypeRemove((int interfaceIndex, int ipProtocolType, string regtype, string domain, uint flags) serviceType)
    {
        using (await this.serviceTypeLock.Enter())
        {
            var key = GetServiceNameKey(
                serviceType.interfaceIndex,
                serviceType.ipProtocolType,
                serviceType.regtype);

            if (!this.serviceTypes.TryGetValue(key, out var existingServiceType))
            {
                Console.WriteLine($"ERROR: resolver was never added: {key}");
                return;
            }

            Console.WriteLine($"decrement usage count {existingServiceType.UsageCount} on resolver {key}");
            --existingServiceType.UsageCount;

            if (existingServiceType.UsageCount > 0)
            {
                return;
            }

            this.RaiseServiceTypeRemoved(existingServiceType.ServiceType);

            Console.WriteLine($"usage count on resolver {key} down to zero, remove it");
            existingServiceType.ServiceType.Dispose();

            this.serviceTypes.Remove(key);
        }
    }

    private static string GetServiceNameKey(int interfaceIndex, int ipProtocolType, string regtype)
    {
        return $"{interfaceIndex}_{ipProtocolType}_{regtype}";
    }
}