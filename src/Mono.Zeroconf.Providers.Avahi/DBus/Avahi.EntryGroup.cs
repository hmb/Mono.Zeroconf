using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]
namespace Mono.Zeroconf.Providers.Avahi.DBus
{
    [DBusInterface("org.freedesktop.Avahi.EntryGroup")]
    interface IEntryGroup : IDBusObject
    {
        Task FreeAsync();
        Task CommitAsync();
        Task ResetAsync();
        Task<int> GetStateAsync();
        Task<bool> IsEmptyAsync();
        Task AddServiceAsync(int Interface, int Protocol, uint Flags, string Name, string Type, string Domain, string Host, ushort Port, byte[][] Txt);
        Task AddServiceSubtypeAsync(int Interface, int Protocol, uint Flags, string Name, string Type, string Domain, string Subtype);
        Task UpdateServiceTxtAsync(int Interface, int Protocol, uint Flags, string Name, string Type, string Domain, byte[][] Txt);
        Task AddAddressAsync(int Interface, int Protocol, uint Flags, string Name, string Address);
        Task AddRecordAsync(int Interface, int Protocol, uint Flags, string Name, ushort Clazz, ushort Type, uint Ttl, byte[] Rdata);
        Task<IDisposable> WatchStateChangedAsync(Action<(int state, string error)> handler, Action<Exception> onError = null);
    }
}