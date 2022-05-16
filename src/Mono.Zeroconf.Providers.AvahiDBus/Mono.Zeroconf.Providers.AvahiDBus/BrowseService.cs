//
// BrowseService.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.Net;
using System.Threading.Tasks;
using AvahiDBus.AvahiObjects;
using Tmds.DBus;

namespace Mono.Zeroconf.Providers.AvahiDBus
{
    public class BrowseService : Service, IResolvableService, IDisposable
    {
        private string full_name;
        private IPHostEntry host_entry;
        private string host_target;
        private short port;
        private bool disposed;
        private IServiceResolver resolver;
        private IDisposable failureWatcher;
        private IDisposable foundWatcher;

        public event ServiceResolvedEventHandler Resolved;
        
        public BrowseService (string name, string regtype, string replyDomain, int @interface, Protocol aprotocol)
            : base (name, regtype, replyDomain, @interface, aprotocol)
        {
        }
        
        public void Dispose ()
        {
            lock (this) {
                disposed = true;
                DisposeResolver ();
            }
        }
        
        private void DisposeResolver ()
        {
            lock (this) {
                if (resolver != null) {
                    failureWatcher.Dispose();
                    foundWatcher.Dispose();;
                    resolver.FreeAsync().GetAwaiter().GetResult();
                    resolver = null;
                }
            }
        }
        
        public async Task Resolve ()
        {
            if (disposed) {
                throw new InvalidOperationException ("The service has been disposed and cannot be resolved. " + 
                    " Perhaps this service was removed?");
            }
            
            //DBusManager.Connection.TrapSignals ();
            
            Console.WriteLine("Resolve called: lock");
            lock (this) {
                if (resolver != null) {
                    throw new InvalidOperationException ("The service is already running a resolve operation");
                }
            }

            Console.WriteLine($"Resolve called: assigning resolver {AvahiInterface} {(int)AvahiProtocol} {Name} {RegType} {ReplyDomain}");
            resolver = await DBusManager.Server.ServiceResolverNewAsync(AvahiInterface, (int)AvahiProtocol, 
                Name ?? String.Empty, RegType ?? String.Empty, ReplyDomain ?? String.Empty, 
                (int)AvahiProtocol, (uint)LookupFlags.None);
            
            Console.WriteLine("Resolve called: WatchFoundAsync/WatchFailureAsync");
            failureWatcher = await resolver.WatchFailureAsync(OnResolveFailure);
            foundWatcher = await resolver.WatchFoundAsync(OnResolveFound);
            
            Console.WriteLine("Resolve called: awaited");
            // DBusManager.Connection.UntrapSignals ();
        }

        protected virtual void OnResolved ()
        {
            ServiceResolvedEventHandler handler = Resolved;
            if (handler != null) {
                handler (this, new ServiceResolvedEventArgs (this));
            }
        }
        
        private void OnResolveFailure (string error)
        {
            DisposeResolver ();
        }

        private void OnResolveFound(
            (int @interface, int protocol, string name, string type, string domain, string host, int aprotocol, string
                address, ushort port, byte[][] txt, uint flags) obj)
        {
            Console.WriteLine("OnResolveFound");
            
            Name = obj.name;
            RegType = obj.type;
            AvahiInterface = obj.@interface;
            AvahiProtocol = (Protocol)obj.protocol;
            ReplyDomain = obj.domain;
            TxtRecord = new TxtRecord (obj.txt);
            
            this.full_name = String.Format ("{0}.{1}.{2}", obj.name.Replace (" ", "\\032"), obj.type, obj.domain);
            this.port = (short)port;
            this.host_target = obj.host;
            
            host_entry = new IPHostEntry ();
            host_entry.AddressList = new IPAddress[1];
            if (IPAddress.TryParse (obj.address, out host_entry.AddressList[0]) && (Protocol)obj.protocol == Protocol.IPv6) {
                host_entry.AddressList[0].ScopeId = obj.@interface;
            }
            host_entry.HostName = obj.host;
            
            OnResolved ();
        }
        
        public string FullName { 
            get { return full_name; }
        }
        
        public IPHostEntry HostEntry { 
            get { return host_entry; } 
        }
        
        public string HostTarget { 
            get { return host_target; } 
        }
        
        public short Port { 
            get { return port; }
        }
    }
}
