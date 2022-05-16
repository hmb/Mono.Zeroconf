using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]
namespace AvahiDBus.AvahiObjects
{
    [DBusInterface("org.freedesktop.Avahi.AddressResolver")]
    interface IAddressResolver : IDBusObject
    {
        Task FreeAsync();
        Task<IDisposable> WatchFoundAsync(Action<(int @interface, int protocol, int aprotocol, string address, string name, uint flags)> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchFailureAsync(Action<string> handler, Action<Exception> onError = null);
    }
}