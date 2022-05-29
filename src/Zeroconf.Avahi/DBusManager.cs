//
// DBusManager.cs
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
using Zeroconf.Avahi.DBus;
using Zeroconf.Avahi.Threading;

internal static class DBusManager
{
    private const string AvahiDbusName = "org.freedesktop.Avahi";
    private const uint MinimumAvahiApiVersion = 515;

    private static readonly AsyncLock s_serverLock = new();

    public static async Task Initialize()
    {
        using (await s_serverLock.Enter("Initialize").ConfigureAwait(false))
        {
            if (Server != null)
            {
                return;
            }

            var connection = Connection.System;

            Server = connection.CreateProxy<IServer>(AvahiDbusName, ObjectPath.Root);
            if (Server == null)
            {
                throw new ApplicationException("Could not find org.freedesktop.Avahi");
            }

            var apiVersion = await Server.GetAPIVersionAsync().ConfigureAwait(false);
            if (apiVersion < MinimumAvahiApiVersion)
            {
                throw new ApplicationException(
                    $"Avahi API version {MinimumAvahiApiVersion} is required, but {apiVersion} is what the server returned.");
            }
        }
    }

    public static IServer? Server { get; private set; }
}