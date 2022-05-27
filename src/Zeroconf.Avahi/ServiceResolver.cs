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
using Microsoft.Extensions.Logging;
using Tmds.DBus;
using Zeroconf.Abstraction;
using Zeroconf.Avahi.DBus;
using Zeroconf.Avahi.Threading;

public class ServiceResolver : Service, IResolvableService, IDisposable
{
    private readonly ILogger logger;
    private readonly AsyncLock serviceLock;

    private IServiceResolver? resolver;
    private IDisposable? foundWatcher;
    private IDisposable? failureWatcher;

    public ServiceResolver(ILoggerFactory loggerFactory, int interfaceIndex, IpProtocolType ipProtocolType, string name, string regType, string replyDomain)
        : base(interfaceIndex, ipProtocolType, name, regType, replyDomain)
    {
        this.logger = loggerFactory.CreateLogger<ServiceResolver>();
        this.serviceLock = new AsyncLock(this.logger);
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

    public uint InterfaceIndex => AvahiUtils.AvahiToZeroconfInterfaceIndex(this.AvahiInterfaceIndex);

    public Abstraction.IpProtocolType IpProtocolType => AvahiUtils.AvahiToZeroconfIpProtocolType(this.AvahiIpProtocolType);

    public ushort Port { get; private set; }

    public async Task Resolve()
    {
        using (await this.serviceLock.Enter("Resolve"))
        {
            if (DBusManager.Server == null)
            {
                throw new ConnectException("no connection to the avahi daemon possible");
            }

            if (this.resolver != null)
            {
                throw new InvalidOperationException("The service is already running a resolve operation");
            }

            this.logger.LogDebug(
                $"Resolve called: assigning resolver {this.AvahiInterfaceIndex} {this.AvahiIpProtocolType} {this.Name} {this.RegType} {this.ReplyDomain}");

            this.resolver = await DBusManager.Server.ServiceResolverNewAsync(
                this.AvahiInterfaceIndex, // TODO Any ?
                (int)this.AvahiIpProtocolType,
                this.Name,
                this.RegType,
                this.ReplyDomain,
                (int)this.AvahiIpProtocolType,
                (uint)LookupFlags.None);

            this.logger.LogDebug("Resolve called: WatchFoundAsync/WatchFailureAsync");
            this.foundWatcher = await this.resolver.WatchFoundAsync(this.OnResolveFound);
            this.failureWatcher = await this.resolver.WatchFailureAsync(this.OnResolveFailure);

            this.logger.LogDebug("Resolve called: awaited");
        }
    }

    public async Task StopResolve()
    {
        using (await this.serviceLock.Enter("StopResolve"))
        {
            if (this.resolver == null)
            {
                return;
            }

            this.logger.LogDebug("found dispose");
            this.foundWatcher?.Dispose();
            this.foundWatcher = null;

            this.logger.LogDebug("failure dispose");
            this.failureWatcher?.Dispose();
            this.failureWatcher = null;
            
            this.logger.LogDebug("FreeAsync");
            await this.resolver.FreeAsync();
            this.resolver = null;
        }
    }

    private void RaiseResolved()
    {
        this.Resolved?.Invoke(this, new ServiceResolvedEventArgs(this));
    }

    private void RaiseResolveFailure(string error)
    {
        this.ResolveFailure?.Invoke(this, error);
    }
    
    private void OnResolveFound((int interfaceIndex, int protocol, string name, string regtype, string domain, string host, int aprotocol, string address, ushort port, byte[][] txt, uint flags) obj)
    {
        this.FullName = $"{obj.name.Replace(" ", "\\032")}.{obj.regtype}.{obj.domain}";
        
        this.AvahiInterfaceIndex = obj.interfaceIndex;
        this.AvahiIpProtocolType = (IpProtocolType)obj.protocol;
        this.Name = obj.name;
        this.RegType = obj.regtype;
        this.ReplyDomain = obj.domain;
        this.HostTarget = obj.host;

        // ReSharper disable once UseObjectOrCollectionInitializer
        this.HostEntry = new IPHostEntry();
        this.HostEntry.HostName = obj.host;
        this.HostEntry.AddressList = new IPAddress[1];
        this.HostEntry.AddressList[0] = ParseIpAddress(obj.address, this.AvahiIpProtocolType, this.AvahiInterfaceIndex);

        this.Port = obj.port;
        this.TxtRecord = new TxtRecord(obj.txt);
        
        this.RaiseResolved();
    }

    private void OnResolveFailure(string error)
    {
        this.RaiseResolveFailure(error);
    }

    private static IPAddress ParseIpAddress(string address, IpProtocolType ipProtocolType, int interfaceIndex)
    {
        if (!IPAddress.TryParse(address, out var ipAddress))
        {
            return IPAddress.None;
        }
        
        if (ipProtocolType == Avahi.IpProtocolType.IPv6)
        {
            ipAddress.ScopeId = interfaceIndex;
        }

        return ipAddress;
    }
}