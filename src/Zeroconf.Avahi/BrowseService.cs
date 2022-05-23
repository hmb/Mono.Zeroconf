//
// BrowseService.cs
//
// Author:
//    Aaron Bockover    <abockover@novell.com>
//    Holger Böhnke     <zeroconf@biz.amarin.de>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.Net;
using System.Threading.Tasks;
using Tmds.DBus;
using Zeroconf.Abstraction;
using Zeroconf.Avahi.DBus;
using Zeroconf.Avahi.Threading;

public class BrowseService : Service, IResolvableService, IDisposable
{
    private readonly AsyncLock serviceLock = new();

    private IServiceResolver? resolver;
    private IDisposable? failureWatcher;
    private IDisposable? foundWatcher;

    public BrowseService(string name, string regtype, string replyDomain, int @interface, Protocol aprotocol)
        : base(name, regtype, replyDomain, @interface, aprotocol)
    {
        this.FullName = string.Empty;
        this.HostEntry = null!;
        this.HostTarget = string.Empty;
        this.Port = 0;
    }

    public void Dispose() => this.StopResolve().GetAwaiter().GetResult();

    public event EventHandler<ServiceResolvedEventArgs>? Resolved;
    public event EventHandler<string>? ResolveFailure;

    public string FullName { get; private set; }

    public IPHostEntry HostEntry { get; private set; }

    public string HostTarget { get; private set; }

    public short Port { get; private set; }

    public async Task Resolve()
    {
        using (await this.serviceLock.Enter())
        {
            if (DBusManager.Server == null)
            {
                throw new ConnectException("no connection to the avahi daemon possible");
            }

            if (this.resolver != null)
            {
                throw new InvalidOperationException("The service is already running a resolve operation");
            }

            Console.WriteLine(
                $"Resolve called: assigning resolver {this.AvahiInterface} {this.AvahiProtocol} {this.Name} {this.RegType} {this.ReplyDomain}");

            this.resolver = await DBusManager.Server.ServiceResolverNewAsync(
                this.AvahiInterface,
                (int)this.AvahiProtocol,
                this.Name,
                this.RegType,
                this.ReplyDomain,
                (int)this.AvahiProtocol,
                (uint)LookupFlags.None);

            Console.WriteLine("Resolve called: WatchFoundAsync/WatchFailureAsync");
            this.foundWatcher = await this.resolver.WatchFoundAsync(this.OnResolveFound);
            this.failureWatcher = await this.resolver.WatchFailureAsync(this.OnResolveFailure);

            Console.WriteLine("Resolve called: awaited");
        }
    }

    public async Task StopResolve()
    {
        using (await this.serviceLock.Enter())
        {
            await this.ClearUnSynchronized();
        }
    }

    private async Task ClearUnSynchronized()
    {
        if (this.resolver == null)
        {
            return;
        }

        this.foundWatcher?.Dispose();
        this.failureWatcher?.Dispose();
        await this.resolver.FreeAsync();

        this.resolver = null;
        this.foundWatcher = null;
        this.failureWatcher = null;
    }

    private void RaiseResolved()
    {
        this.Resolved?.Invoke(this, new ServiceResolvedEventArgs(this));
    }

    private void RaiseResolveFailure(string error)
    {
        this.ResolveFailure?.Invoke(this, error);
    }
    
    private void OnResolveFound(
        (int @interface, int protocol, string name, string type, string domain, string host, int aprotocol, string
            address, ushort port, byte[][] txt, uint flags) obj)
    {
        this.Name = obj.name;
        this.RegType = obj.type;
        this.AvahiInterface = obj.@interface;
        this.AvahiProtocol = (Protocol)obj.protocol;
        this.ReplyDomain = obj.domain;
        this.TxtRecord = new TxtRecord(obj.txt);

        this.FullName = $"{obj.name.Replace(" ", "\\032")}.{obj.type}.{obj.domain}";
        this.Port = (short)obj.port;
        this.HostTarget = obj.host;

        // ReSharper disable once UseObjectOrCollectionInitializer
        this.HostEntry = new IPHostEntry();

        this.HostEntry.AddressList = new IPAddress[1];

        if (IPAddress.TryParse(obj.address, out var ipAddress))
        {
            this.HostEntry.AddressList[0] = ipAddress;
            if ((Protocol)obj.protocol == Protocol.IPv6)
            {
                this.HostEntry.AddressList[0].ScopeId = obj.@interface;
            }
        }
        else
        {
            this.HostEntry.AddressList[0] = IPAddress.None;
        }

        this.HostEntry.HostName = obj.host;

        this.RaiseResolved();
    }

    private void OnResolveFailure(string error)
    {
        this.RaiseResolveFailure(error);
    }
}