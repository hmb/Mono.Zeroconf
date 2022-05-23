namespace Mono.Zeroconf.Providers.Avahi.Threading;

using System;
using System.Threading;
using System.Threading.Tasks;

public class AsyncLock : SemaphoreSlim
{
    public AsyncLock() : base(1, 1)
    {
    }
}

public static class SemaphoreSlimExtension
{
    public static async Task<IDisposable> Enter(this SemaphoreSlim semaphore)
    {
        var wrapper = new LockHolder(semaphore);
        await wrapper.Semaphore.WaitAsync();
        return wrapper;
    }

    private sealed class LockHolder : IDisposable
    {
        public SemaphoreSlim Semaphore { get; }
        public LockHolder(SemaphoreSlim semaphore) => this.Semaphore = semaphore;
        public void Dispose() => this.Semaphore.Release();
    }
}