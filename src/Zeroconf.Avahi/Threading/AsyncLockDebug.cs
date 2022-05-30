namespace Zeroconf.Avahi.Threading;

using Microsoft.Extensions.Logging;

public class AsyncLockDebug : SemaphoreSlim
{
    public AsyncLockDebug(ILogger logger, string name) : base(1, 1)
    {
        this.Logger = logger;
        this.Name = name;
    }

    public ILogger Logger { get; }
    public string Name { get; }
}

public static class AsyncLockDebugExtension
{
    public static async Task<IDisposable> Enter(this AsyncLockDebug semaphore, string description)
    {
        semaphore.Logger.LogTrace("semaphore {Name} entering {Description}",semaphore.Name, description);
        var wrapper = new LockHolderDebug(semaphore, description);
        await wrapper.Semaphore.WaitAsync().ConfigureAwait(false);
        semaphore.Logger.LogTrace("semaphore {Name} entered {Description}", semaphore.Name, description);
        return wrapper;
    }

    private sealed class LockHolderDebug : IDisposable
    {
        private readonly string description;
        public AsyncLockDebug Semaphore { get; }

        public LockHolderDebug(AsyncLockDebug semaphore, string description)
        {
            this.Semaphore = semaphore;
            this.description = description;
        }

        public void Dispose()
        {
            this.Semaphore.Logger.LogTrace("semaphore {Name} leaving {Description}", this.Semaphore.Name, this.description);
            this.Semaphore.Release();
            this.Semaphore.Logger.LogTrace("semaphore {Name} left {Description}", this.Semaphore.Name, this.description);
        }
    }
}