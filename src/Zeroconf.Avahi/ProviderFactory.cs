//
// ProviderFactory.cs
//
// Authors:
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
using Zeroconf.Abstraction;
using IServiceBrowser = Zeroconf.Abstraction.IServiceBrowser;
using IServiceTypeBrowser = Zeroconf.Abstraction.IServiceTypeBrowser;

public class ProviderFactory : IProviderFactory
{
    private readonly ILoggerFactory loggerFactory;

    public ProviderFactory(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
    }

    public async Task Initialize()
    {
        await AvahiInit.Initialize();
    }

    public IServiceTypeBrowser CreateServiceTypeBrowser(
        uint interfaceIndex,
        Abstraction.IpProtocolType ipProtocolType,
        string replyDomain)
    {
        return new ServiceTypeBrowser(
            this.loggerFactory,
            AvahiUtils.ZeroconfToAvahiInterfaceIndex(interfaceIndex),
            AvahiUtils.ZeroconfToAvahiIpAddressProtocol(ipProtocolType),
            replyDomain);
    }

    public IServiceBrowser CreateServiceBrowser(
        uint interfaceIndex,
        Abstraction.IpProtocolType ipProtocolType,
        string regtype,
        string domain)
    {
        return new ServiceBrowser(
            this.loggerFactory,
            AvahiUtils.ZeroconfToAvahiInterfaceIndex(interfaceIndex),
            AvahiUtils.ZeroconfToAvahiIpAddressProtocol(ipProtocolType),
            regtype,
            domain);
    }

    public IResolvableService CreateServiceResolver(
        uint interfaceIndex,
        Abstraction.IpProtocolType ipProtocolType,
        string name,
        string regtype,
        string domain)
    {
        return new ServiceResolver(
            this.loggerFactory,
            AvahiUtils.ZeroconfToAvahiInterfaceIndex(interfaceIndex),
            AvahiUtils.ZeroconfToAvahiIpAddressProtocol(ipProtocolType),
            name,
            regtype,
            domain);
    }
    
    public IRegisterService CreateRegisterService()
    {
        return new RegisterService();
    }

    public ITxtRecord CreateTxtRecord()
    {
        return new TxtRecord();
    }
}