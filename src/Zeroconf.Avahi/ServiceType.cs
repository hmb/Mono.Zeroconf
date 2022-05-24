namespace Zeroconf.Avahi;

using Zeroconf.Abstraction;

public class ServiceType : IServiceType, IDisposable
{
    public void Dispose()
    {
    }

    public ServiceType(int avahiInterfaceIndex, IpProtocolType protocolType, string regtype, string domain)
    {
        this.InterfaceIndex = AvahiUtils.AvahiToZeroconfInterfaceIndex(avahiInterfaceIndex);
        this.ProtocolType = AvahiUtils.AvahiToZeroconfIpAddressProtocol(protocolType);
        this.Regtype = regtype;
        this.Domain = domain;
    }
    
    public uint InterfaceIndex { get; }
    public Abstraction.IpProtocolType ProtocolType { get; }
    public string Regtype { get; }
    public string Domain { get; }
}