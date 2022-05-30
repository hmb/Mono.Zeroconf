namespace Zeroconf.Avahi.Threading;

public class AsyncLock : SemaphoreSlim, IAsyncLock
{
    public AsyncLock() : base(1, 1)
    {
    }

    public async Task<IDisposable> Enter()
    {
        var wrapper = new LockHolder(this);
        await wrapper.Semaphore.WaitAsync().ConfigureAwait(false);
        return wrapper;
    }

    public async Task<IDisposable> Enter(string _)
    {
        var wrapper = new LockHolder(this);
        await wrapper.Semaphore.WaitAsync().ConfigureAwait(false);
        return wrapper;
    }

    private sealed class LockHolder : IDisposable
    {
        public AsyncLock Semaphore { get; }
        public LockHolder(AsyncLock semaphore) => this.Semaphore = semaphore;
        public void Dispose() => this.Semaphore.Release();
    }
}