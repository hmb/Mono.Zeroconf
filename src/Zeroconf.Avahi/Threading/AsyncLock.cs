namespace Zeroconf.Avahi.Threading;

using Microsoft.Extensions.Logging;

public class AsyncLock : SemaphoreSlim
{
    public AsyncLock() : base(1, 1)
    {
    }

    public AsyncLock(ILogger _, string __) : base(1, 1)
    {
    }
}

public static class AsyncLockExtension
{
    public static async Task<IDisposable> Enter(this AsyncLock semaphore)
    {
        var wrapper = new LockHolder(semaphore);
        await wrapper.Semaphore.WaitAsync().ConfigureAwait(false);
        return wrapper;
    }

    public static async Task<IDisposable> Enter(this AsyncLock semaphore, string _)
    {
        var wrapper = new LockHolder(semaphore);
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