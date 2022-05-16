//
// ServiceBrowser.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Mono.Zeroconf;

using Tmds.DBus;

namespace Mono.Zeroconf.Providers.AvahiDBus
{
    public class ServiceBrowser : IServiceBrowser
    {
        public event ServiceBrowseEventHandler ServiceAdded;
        public event ServiceBrowseEventHandler ServiceRemoved;
    
        private global::AvahiDBus.AvahiObjects.IServiceBrowser service_browser;
        private Dictionary<string, BrowseService> services = new Dictionary<string, BrowseService> ();
        private IDisposable itemNewWatcher;
        private IDisposable itemRemoveWatcher;

        public void Dispose ()
        {
            lock (this) {
                if (service_browser != null) {
                    itemNewWatcher.Dispose();
                    itemRemoveWatcher.Dispose();
                    service_browser.FreeAsync().GetAwaiter().GetResult();
                }
                
                if (services.Count > 0) {
                    foreach (BrowseService service in services.Values) {
                        service.Dispose ();
                    }
                    services.Clear ();
                }
            }
        }
    
        public async Task Browse(uint interfaceIndex, AddressProtocol addressProtocol, string regtype, string domain)
        {
            // DBusManager.Connection.TrapSignals ();
            
            lock (this) {
                Dispose ();
                
                service_browser = DBusManager.Server.ServiceBrowserNewAsync (
                    AvahiUtils.FromMzcInterface (interfaceIndex), 
                    (int)AvahiUtils.FromMzcProtocol (addressProtocol), 
                    regtype ?? string.Empty, domain ?? string.Empty, 
                    (uint)LookupFlags.None).GetAwaiter().GetResult();
            }

            itemNewWatcher = await service_browser.WatchItemNewAsync(OnItemNew);
            itemRemoveWatcher = await service_browser.WatchItemRemoveAsync(OnItemRemove);
            
            // DBusManager.Bus.UntrapSignals ();
        }
        
        protected virtual void OnServiceAdded (BrowseService service)
        {
            ServiceBrowseEventHandler handler = ServiceAdded;
            if (handler != null) {
                handler (this, new ServiceBrowseEventArgs (service));
            }
        }
        
        protected virtual void OnServiceRemoved (BrowseService service)
        {
            ServiceBrowseEventHandler handler = ServiceRemoved;
            if (handler != null) {
                handler (this, new ServiceBrowseEventArgs (service));
            }
        }
        
        public IEnumerator<IResolvableService> GetEnumerator ()
        {
            lock (this) {
                foreach (IResolvableService service in services.Values) {
                    yield return service;
                }
            }
        }
        
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }
        
        private void OnItemNew((int @interface, int protocol, string name, string type, string domain, uint flags) obj)
        {
            lock (this) {
                var service = new BrowseService (obj.name, obj.type, obj.domain, obj.@interface, (Protocol)obj.protocol);
                
                if (services.ContainsKey (obj.name)) {
                    services[obj.name].Dispose ();
                    services[obj.name] = service;
                } else {
                    services.Add (obj.name, service);
                }
                
                OnServiceAdded (service);
            }
        }
        
        private void OnItemRemove((int @interface, int protocol, string name, string type, string domain, uint flags) obj)
        {
            lock (this) {
                var service = new BrowseService(obj.name, obj.type, obj.domain, obj.@interface, (Protocol)obj.protocol);
                
                if (services.ContainsKey (obj.name)) {
                    services[obj.name].Dispose ();
                    services.Remove (obj.name);
                }
                
                OnServiceRemoved (service);
                service.Dispose ();
            }
        }
    }
}
