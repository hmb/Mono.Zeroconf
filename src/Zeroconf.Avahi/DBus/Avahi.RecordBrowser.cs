using System.Runtime.CompilerServices;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]
namespace Zeroconf.Avahi.DBus;

using System;
using System.Threading.Tasks;
using Tmds.DBus;

[DBusInterface("org.freedesktop.Avahi.RecordBrowser")]
interface IRecordBrowser : IDBusObject
{
    Task FreeAsync();
    Task<IDisposable> WatchItemNewAsync(Action<(int @interface, int protocol, string name, ushort clazz, ushort type, byte[] rdata, uint flags)> handler, Action<Exception>? onError = null);
    Task<IDisposable> WatchItemRemoveAsync(Action<(int @interface, int protocol, string name, ushort clazz, ushort type, byte[] rdata, uint flags)> handler, Action<Exception>? onError = null);
    Task<IDisposable> WatchFailureAsync(Action<string> handler, Action<Exception>? onError = null);
    Task<IDisposable> WatchAllForNowAsync(Action handler, Action<Exception>? onError = null);
    Task<IDisposable> WatchCacheExhaustedAsync(Action handler, Action<Exception>? onError = null);
}