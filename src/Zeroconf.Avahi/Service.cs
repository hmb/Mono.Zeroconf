//
// Service.cs
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

using Zeroconf.Abstraction;

public class Service : IService
{
    protected Service()
    {
        this.AvahiInterfaceIndex = AvahiUtils.AvahiInterfaceIndexAny;
        this.AvahiIpProtocolType = IpProtocolType.Unspecified;
        this.Name = string.Empty;
        this.RegType = string.Empty;
        this.ReplyDomain = string.Empty;
    }

    protected Service(int interfaceIndex, IpProtocolType ipProtocolType, string name, string regType, string replyDomain)
    {
        this.AvahiInterfaceIndex = interfaceIndex;
        this.AvahiIpProtocolType = ipProtocolType;
        this.Name = name;
        this.RegType = regType;
        this.ReplyDomain = replyDomain;
    }

    public string Name { get; set; }

    public string RegType { get; set; }

    public string ReplyDomain { get; set; }

    public ITxtRecord? TxtRecord { get; set; }

    protected int AvahiInterfaceIndex { get; set; }

    protected IpProtocolType AvahiIpProtocolType { get; set; }
}