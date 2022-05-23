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

namespace Zeroconf.Avahi
{
    using Zeroconf.Abstraction;

    public static class AvahiUtils
    {
        private const int MonoZeroconfAnyInterface = 0;
        private const int AvahiAnyInterface = -1;

        public static Protocol FromMzcProtocol(AddressProtocol addressProtocol)
        {
            return addressProtocol switch
            {
                AddressProtocol.IPv4 => Protocol.IPv4,
                AddressProtocol.IPv6 => Protocol.IPv6,
                _ => Protocol.Unspecified
            };
        }

        public static AddressProtocol ToMzcProtocol(Protocol addressProtocol)
        {
            return addressProtocol switch
            {
                Protocol.IPv4 => AddressProtocol.IPv4,
                Protocol.IPv6 => AddressProtocol.IPv6,
                _ => AddressProtocol.Any
            };
        }

        public static int FromMzcInterface(uint @interface)
        {
            return @interface switch
            {
                MonoZeroconfAnyInterface => AvahiAnyInterface,
                _ => (int)@interface - 1
            };
        }

        public static uint ToMzcInterface(int @interface)
        {
            return @interface switch
            {
                AvahiAnyInterface => MonoZeroconfAnyInterface,
                _ => (uint)@interface + 1
            };
        }

        public static ServiceErrorCode ErrorCodeToServiceError(ErrorCode error)
        {
            return error switch
            {
                ErrorCode.Ok => ServiceErrorCode.None,
                ErrorCode.Collision => ServiceErrorCode.NameConflict,
                _ => ServiceErrorCode.Unknown
            };
        }
    }
}