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
using System.Threading.Tasks;
using Tmds.DBus;
using Zeroconf.Abstraction;
using Zeroconf.Avahi.DBus;
using Zeroconf.Avahi.Threading;

public class RegisterService : Service, IRegisterService
{
    private readonly AsyncLock serviceLock = new(); 
    private IEntryGroup? entryGroup;
    private IDisposable? stateChangeWatcher;

    public RegisterService()
    {
    }
    
    public RegisterService(string name, string regtype, string replyDomain, int interfaceIndex, IpProtocolType ipProtocolType)
        : base(name, regtype, replyDomain, interfaceIndex, ipProtocolType)
    {
    }

    public void Dispose()
    {
        using (this.serviceLock.Enter().GetAwaiter().GetResult())
        {
            if (this.entryGroup == null)
            {
                return;
            }

            this.stateChangeWatcher?.Dispose();
            this.stateChangeWatcher = null;
            
            this.entryGroup.ResetAsync().GetAwaiter().GetResult();
            this.entryGroup.FreeAsync().GetAwaiter().GetResult();
            this.entryGroup = null;
        }
    }

    public event EventHandler<RegisterServiceEventArgs>? Response;

    public short Port
    {
        get => (short)this.UPort;
        set => this.UPort = (ushort)value;
    }

    public ushort UPort { get; set; }

    public async Task Register()
    {
        using (await this.serviceLock.Enter())
        {
            if (DBusManager.Server == null)
            {
                throw new ConnectException("no connection to the avahi daemon possible");
            }
            
            if (await DBusManager.Server.GetStateAsync() != AvahiServerState.Running)
            {
                throw new ApplicationException("Avahi server is not rRunning");
            }

            if (this.entryGroup == null)
            {
                this.entryGroup = await DBusManager.Server.EntryGroupNewAsync();
            }
            else
            {
                this.stateChangeWatcher?.Dispose();
                await this.entryGroup.ResetAsync();
            }

            this.stateChangeWatcher = await this.entryGroup.WatchStateChangedAsync(this.OnEntryGroupStateChanged);

            if (this.entryGroup == null)
            {
                throw new ApplicationException("no avahi entry group present");
            }
            
            var avahiTxtRecord = this.TxtRecord?.Render() ?? Array.Empty<byte[]>();

            await this.entryGroup.AddServiceAsync(
                this.AvahiInterfaceIndex,
                (int)this.AvahiIpProtocolType,
                (uint)PublishFlags.None,
                this.Name,
                this.RegType,
                this.ReplyDomain,
                string.Empty,
                this.UPort,
                avahiTxtRecord);

            await this.entryGroup.CommitAsync();
        }
    }

    private void OnEntryGroupStateChanged((int state, string error) obj)
    {
        // TODO this is very strange code if there's no attached event handler the function throws
        // which is pretty useless in an event handler
        switch ((EntryGroupState)obj.state)
        {
            case EntryGroupState.Collision:
                if (!this.RaiseResponse(ErrorCode.Collision))
                {
                    throw new ApplicationException();
                }
                break;
            case EntryGroupState.Failure:
                if (!this.RaiseResponse(ErrorCode.Failure))
                {
                    throw new ApplicationException();
                }
                break;
            case EntryGroupState.Established:
                this.RaiseResponse(ErrorCode.Ok);
                break;
        }
    }

    private bool RaiseResponse(ErrorCode errorCode)
    {
        var args = new RegisterServiceEventArgs
        {
            Service = this,
            IsRegistered = false,
            ServiceError = AvahiUtils.AvahiToZeroconfErrorCode(errorCode)
        };

        if (errorCode == ErrorCode.Ok)
        {
            args.IsRegistered = true;
        }

        this.Response?.Invoke(this, args);

        // TODO check with the TODO above in OnEntryGroupStateChanged
        return this.Response != null;
    }
}