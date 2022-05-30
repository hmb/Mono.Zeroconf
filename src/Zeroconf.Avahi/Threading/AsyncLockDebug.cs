namespace Zeroconf.Avahi.Threading;

using Microsoft.Extensions.Logging;

public class AsyncLockDebug : SemaphoreSlim, IAsyncLock
{
    private readonly ILogger logger;
    private readonly string name;

    public AsyncLockDebug(ILogger logger, string name) : base(1, 1)
    {
        this.logger = logger;
        this.name = name;
    }

    public async Task<IDisposable> Enter()
    {
        return await this.Enter("- no description -").ConfigureAwait(false);
    }

    public async Task<IDisposable> Enter(string description)
    {
        this.logger.LogTrace("semaphore {Name} entering {Description}", this.name, description);
        var wrapper = new LockHolder(this, description);
        await wrapper.Semaphore.WaitAsync().ConfigureAwait(false);
        this.logger.LogTrace("semaphore {Name} entered {Description}", this.name, description);
        return wrapper;
    }

    private sealed class LockHolder : IDisposable
    {
        private readonly string description;
        public AsyncLockDebug Semaphore { get; }

        public LockHolder(AsyncLockDebug semaphore, string description)
        {
            this.Semaphore = semaphore;
            this.description = description;
        }

        public void Dispose()
        {
            this.Semaphore.logger.LogTrace("semaphore {Name} leaving {Description}", this.Semaphore.name, this.description);
            this.Semaphore.Release();
            this.Semaphore.logger.LogTrace("semaphore {Name} left {Description}", this.Semaphore.name, this.description);
        }
    }
}