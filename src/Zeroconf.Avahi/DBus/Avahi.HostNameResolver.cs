using System.Runtime.CompilerServices;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]
namespace Zeroconf.Avahi.DBus;

using System;
using System.Threading.Tasks;
using Tmds.DBus;

[DBusInterface("org.freedesktop.Avahi.HostNameResolver")]
interface IHostNameResolver : IDBusObject
{
    Task FreeAsync();
    Task<IDisposable> WatchFoundAsync(Action<(int @interface, int protocol, string name, int aprotocol, string address, uint flags)> handler, Action<Exception> onError = null);
    Task<IDisposable> WatchFailureAsync(Action<string> handler, Action<Exception> onError = null);
}