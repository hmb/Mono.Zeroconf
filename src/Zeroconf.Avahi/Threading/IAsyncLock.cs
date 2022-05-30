namespace Zeroconf.Avahi.Threading;

public interface IAsyncLock
{
    Task<IDisposable> Enter();
    Task<IDisposable> Enter(string description);
}