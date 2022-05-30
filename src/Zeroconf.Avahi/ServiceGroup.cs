//
// ServiceGroup.cs
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

using Microsoft.Extensions.Logging;
using Tmds.DBus;
using Zeroconf.Abstraction;
using Zeroconf.Avahi.DBus;
using Zeroconf.Avahi.Threading;

public class ServiceGroup : IServiceGroup
{
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger logger;
    private readonly IAsyncLock serviceLock;
    
    private IEntryGroup? entryGroup;
    private IDisposable? stateChangeWatcher;

    public ServiceGroup(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
        this.logger = loggerFactory.CreateLogger<ILoggerFactory>();
        this.serviceLock = new AsyncLockDebug(this.logger, "serviceLock");
    }

    public void Dispose()
    {
        this.Terminate().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public event EventHandler<RegisterServiceEventArgs>? Response;

    public async Task Initialize()
    {
        using (await this.serviceLock.Enter("Initialize").ConfigureAwait(false))
        {
            if (DBusManager.Server == null)
            {
                throw new ConnectException("no connection to the avahi daemon possible");
            }

            if (await DBusManager.Server.GetStateAsync().ConfigureAwait(false) != AvahiServerState.Running)
            {
                throw new ApplicationException("Avahi server is not rRunning");
            }

            if (this.entryGroup != null)
            {
                throw new InvalidOperationException("The service is already registered");
            }

            this.entryGroup = await DBusManager.Server.EntryGroupNewAsync().ConfigureAwait(false);
            this.stateChangeWatcher = await this.entryGroup.WatchStateChangedAsync(this.OnEntryGroupStateChanged).ConfigureAwait(false);

            if (this.entryGroup == null)
            {
                throw new ApplicationException("no avahi entry group present");
            }
        }
    }

    public async Task Terminate()
    {
        using (await this.serviceLock.Enter("Terminate").ConfigureAwait(false))
        {
            if (this.entryGroup == null)
            {
                return;
            }

            this.stateChangeWatcher?.Dispose();
            this.stateChangeWatcher = null;

            await this.entryGroup.FreeAsync().ConfigureAwait(false);
            this.entryGroup = null;
        }
    }

    public async Task AddServiceAsync(
        uint interfaceIndex,
        Abstraction.IpProtocolType ipProtocolType,
        string name,
        string regType,
        string replyDomain,
        string target,
        ushort port,
        ITxtRecord? txtRecord)
    {
        using (await this.serviceLock.Enter("AddServiceAsync").ConfigureAwait(false))
        {
            if (this.entryGroup == null)
            {
                throw new ApplicationException("no avahi entry group present");
            }

            var avahiTxtRecord = txtRecord?.Render() ?? Array.Empty<byte[]>();

            await this.entryGroup.AddServiceAsync(
                AvahiUtils.ZeroconfToAvahiInterfaceIndex(interfaceIndex),
                AvahiUtils.ZeroconfToAvahiIpProtocolType(ipProtocolType).ToNativeAvahi(),
                PublishFlags.None.ToNativeAvahi(),
                name,
                regType,
                replyDomain,
                target,
                port,
                avahiTxtRecord).ConfigureAwait(false);
        }
    }

    public async Task CommitAsync()
    {
        using (await this.serviceLock.Enter("CommitAsync").ConfigureAwait(false))
        {

            if (this.entryGroup == null)
            {
                throw new ApplicationException("no avahi entry group present");
            }

            await this.entryGroup.CommitAsync().ConfigureAwait(false);
        }
    }
    
    public async Task ResetAsync()
    {
        using (await this.serviceLock.Enter("ResetAsync").ConfigureAwait(false))
        {
            if (this.entryGroup == null)
            {
                throw new ApplicationException("no avahi entry group present");
            }

            await this.entryGroup.ResetAsync().ConfigureAwait(false);
            
            // this apparently is a bug in avahi (or a known issue, or broken by design):
            // after reset and republish the services do not get republished
            // if only payload data changes (port, target and txt)
            await Task.Delay(1000).ConfigureAwait(false);
        }
    }

    private void OnEntryGroupStateChanged((int state, string error) obj)
    {
        this.logger.LogDebug("OnEntryGroupStateChanged {State} {Error}", (EntryGroupState)obj.state, obj.error);
        
        switch ((EntryGroupState)obj.state)
        {
            case EntryGroupState.Collision:
                this.RaiseResponse(ErrorCode.Collision);
                break;
            
            case EntryGroupState.Failure:
                this.RaiseResponse(ErrorCode.Failure);
                break;
            
            case EntryGroupState.Established:
                this.RaiseResponse(ErrorCode.Ok);
                break;
            
            case EntryGroupState.Uncommited:
                break;
            
            case EntryGroupState.Registering:
                break;

            // no default
            // default:
        }
    }

    private void RaiseResponse(ErrorCode errorCode)
    {
        var args = new RegisterServiceEventArgs
        {
            //Service = this,
            IsRegistered = errorCode == ErrorCode.Ok,
            ServiceError = AvahiUtils.AvahiToZeroconfErrorCode(errorCode)
        };

        this.Response?.Invoke(this, args);
    }
}