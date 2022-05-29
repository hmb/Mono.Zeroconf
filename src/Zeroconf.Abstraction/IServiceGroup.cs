namespace Zeroconf.Abstraction;

public interface IServiceGroup : IDisposable
{
    public event EventHandler<RegisterServiceEventArgs>? Response;
    
    public Task Initialize();

    public Task Terminate();
    
    public Task AddServiceAsync(
        uint interfaceIndex,
        IpProtocolType ipProtocolType,
        string name,
        string regType,
        string replyDomain,
        string target,
        ushort port,
        ITxtRecord? txtRecord);

    public Task CommitAsync();

    public Task ResetAsync();
}