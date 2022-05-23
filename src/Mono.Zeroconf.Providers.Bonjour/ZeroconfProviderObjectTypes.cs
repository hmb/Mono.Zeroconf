//
// ZeroconfProvider.cs
//
// Authors:
//    Aaron Bockover  <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc (http://www.novell.com)
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

using Mono.Zeroconf.Providers.Abstraction;
using Mono.Zeroconf.Providers.Bonjour;

[assembly:ZeroconfProvider(typeof(ZeroconfProviderObjectTypes))]

namespace Mono.Zeroconf.Providers.Bonjour;

using System;
using Mono.Zeroconf.Providers.Abstraction;

public static class Zeroconf
{
    public static void Initialize()
    {
        var error = Native.DNSServiceCreateConnection(out var sd_ref);
            
        if(error != ServiceError.NoError) {
            throw new ServiceErrorException(error);
        }
            
        sd_ref.Deallocate();
    }
}

public class ZeroconfProviderObjectTypes : IZeroconfProviderObjectTypes
{
    public void Initialize()
    {
        Zeroconf.Initialize();
    }
        
    public Type ServiceBrowser => typeof(ServiceBrowser);
    public Type RegisterService => typeof(RegisterService);
    public Type TxtRecord => typeof(TxtRecord);
}