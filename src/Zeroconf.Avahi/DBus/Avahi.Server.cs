using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]
namespace Mono.Zeroconf.Providers.Avahi.DBus
{
    [DBusInterface("org.freedesktop.Avahi.Server")]
    interface IServer : IDBusObject
    {
        Task<string> GetVersionStringAsync();
        Task<uint> GetAPIVersionAsync();
        Task<string> GetHostNameAsync();
        Task SetHostNameAsync(string Name);
        Task<string> GetHostNameFqdnAsync();
        Task<string> GetDomainNameAsync();
        Task<bool> IsNSSSupportAvailableAsync();
        Task<AvahiServerState> GetStateAsync();
        Task<uint> GetLocalServiceCookieAsync();
        Task<string> GetAlternativeHostNameAsync(string Name);
        Task<string> GetAlternativeServiceNameAsync(string Name);
        Task<string> GetNetworkInterfaceNameByIndexAsync(int Index);
        Task<int> GetNetworkInterfaceIndexByNameAsync(string Name);
        Task<(int @interface, int protocol, string name, int aprotocol, string address, uint flags)> ResolveHostNameAsync(int Interface, int Protocol, string Name, int Aprotocol, uint Flags);
        Task<(int @interface, int protocol, int aprotocol, string address, string name, uint flags)> ResolveAddressAsync(int Interface, int Protocol, string Address, uint Flags);
        Task<(int @interface, int protocol, string name, string type, string domain, string host, int aprotocol, string address, ushort port, byte[][] txt, uint flags)> ResolveServiceAsync(int Interface, int Protocol, string Name, string Type, string Domain, int Aprotocol, uint Flags);
        Task<IEntryGroup> EntryGroupNewAsync();
        Task<IDomainBrowser> DomainBrowserNewAsync(int Interface, int Protocol, string Domain, int Btype, uint Flags);
        Task<IServiceTypeBrowser> ServiceTypeBrowserNewAsync(int Interface, int Protocol, string Domain, uint Flags);
        Task<IServiceBrowser> ServiceBrowserNewAsync(int Interface, int Protocol, string Type, string Domain, uint Flags);
        Task<IServiceResolver> ServiceResolverNewAsync(int Interface, int Protocol, string Name, string Type, string Domain, int Aprotocol, uint Flags);
        Task<IHostNameResolver> HostNameResolverNewAsync(int Interface, int Protocol, string Name, int Aprotocol, uint Flags);
        Task<IAddressResolver> AddressResolverNewAsync(int Interface, int Protocol, string Address, uint Flags);
        Task<IRecordBrowser> RecordBrowserNewAsync(int Interface, int Protocol, string Name, ushort Clazz, ushort Type, uint Flags);
        Task<IDisposable> WatchStateChangedAsync(Action<(int state, string error)> handler, Action<Exception> onError = null);
    }
}