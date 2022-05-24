namespace Zeroconf.Abstraction;

public interface IServiceType
{
    uint InterfaceIndex { get; }
    IpProtocolType ProtocolType { get; }
    string Regtype { get; }
    string Domain { get; }
}