using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]
namespace AvahiDBus.AvahiObjects
{
    [DBusInterface("org.freedesktop.Avahi.ServiceBrowser")]
    interface IServiceBrowser : IDBusObject
    {
        Task FreeAsync();
        Task<IDisposable> WatchItemNewAsync(Action<(int @interface, int protocol, string name, string type, string domain, uint flags)> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchItemRemoveAsync(Action<(int @interface, int protocol, string name, string type, string domain, uint flags)> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchFailureAsync(Action<string> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchAllForNowAsync(Action handler, Action<Exception> onError = null);
        Task<IDisposable> WatchCacheExhaustedAsync(Action handler, Action<Exception> onError = null);
    }
}