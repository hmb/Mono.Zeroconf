//
// ServiceBrowser.cs
//
// Authors:
//    Aaron Bockover    <abockover@novell.com>
//    Holger Böhnke     <zeroconf@biz.amarin.de>
//
// Copyright (C) 2006-2007 Novell, Inc (http://www.novell.com)
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
using Mono.Zeroconf.Providers;

namespace Mono.Zeroconf;

public class ServiceBrowser : IServiceBrowser
{
    private readonly IServiceBrowser browser;

    public ServiceBrowser()
    {
        this.browser = (IServiceBrowser)Activator.CreateInstance(ProviderFactory.SelectedProvider.ServiceBrowser);
    }

    public void Dispose()
    {
        this.browser.Dispose();
    }

    public event ServiceBrowseEventHandler ServiceAdded
    {
        add => this.browser.ServiceAdded += value;
        remove => this.browser.ServiceAdded -= value;
    }

    public event ServiceBrowseEventHandler ServiceRemoved
    {
        add => this.browser.ServiceRemoved += value;
        remove => this.browser.ServiceRemoved -= value;
    }

    public IEnumerator<IResolvableService> GetEnumerator()
    {
        return this.browser.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.browser.GetEnumerator();
    }
    
    public async Task Browse(uint interfaceIndex, AddressProtocol addressProtocol, string regtype, string domain)
    {
        await this.browser.Browse(interfaceIndex, addressProtocol, regtype, domain ?? "local");
    }

    public async Task Browse(uint interfaceIndex, string regtype, string domain)
    {
        await this.Browse(interfaceIndex, AddressProtocol.Any, regtype, domain);
    }

    public async Task Browse(AddressProtocol addressProtocol, string regtype, string domain)
    {
        await this.Browse(0, addressProtocol, regtype, domain);
    }

    public async Task Browse(string regtype, string domain)
    {
        await this.Browse(0, AddressProtocol.Any, regtype, domain);
    }
}