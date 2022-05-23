using System.Runtime.CompilerServices;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]
namespace Zeroconf.Avahi.DBus;

using Tmds.DBus;

[DBusInterface("org.freedesktop.Avahi.AddressResolver")]
interface IAddressResolver : IDBusObject
{
    Task FreeAsync();
    Task<IDisposable> WatchFoundAsync(Action<(int @interface, int protocol, int aprotocol, string address, string name, uint flags)> handler, Action<Exception>? onError = null);
    Task<IDisposable> WatchFailureAsync(Action<string> handler, Action<Exception>? onError = null);
}