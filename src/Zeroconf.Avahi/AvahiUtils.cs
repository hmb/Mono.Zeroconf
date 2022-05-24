//
// AvahiUtils.cs
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

public static class AvahiUtils
{
    public const int AvahiInterfaceIndexAny = -1;

    public static IpProtocolType ZeroconfToAvahiIpAddressProtocol(Abstraction.IpProtocolType ipProtocolType)
    {
        return ipProtocolType switch
        {
            Abstraction.IpProtocolType.IPv4 => IpProtocolType.IPv4,
            Abstraction.IpProtocolType.IPv6 => IpProtocolType.IPv6,
            _ => IpProtocolType.Unspecified
        };
    }

    public static Abstraction.IpProtocolType AvahiToZeroconfIpAddressProtocol(IpProtocolType ipProtocolType)
    {
        return ipProtocolType switch
        {
            IpProtocolType.IPv4 => Abstraction.IpProtocolType.IPv4,
            IpProtocolType.IPv6 => Abstraction.IpProtocolType.IPv6,
            _ => Abstraction.IpProtocolType.Any
        };
    }

    public static int ZeroconfToAvahiInterfaceIndex(uint interfaceIndex)
    {
        return interfaceIndex switch
        {
            ZeroconfConstants.InterfaceIndexAny => AvahiInterfaceIndexAny,
            _ => (int)interfaceIndex
        };
    }

    public static uint AvahiToZeroconfInterfaceIndex(int interfaceIndex)
    {
        return interfaceIndex switch
        {
            AvahiInterfaceIndexAny => ZeroconfConstants.InterfaceIndexAny,
            _ => (uint)interfaceIndex
        };
    }

    public static ServiceErrorCode AvahiToZeroconfErrorCode(ErrorCode error)
    {
        return error switch
        {
            ErrorCode.Ok => ServiceErrorCode.None,
            ErrorCode.Collision => ServiceErrorCode.NameConflict,
            _ => ServiceErrorCode.Unknown
        };
    }
}