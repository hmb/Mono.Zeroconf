namespace Zeroconf.Avahi.Threading;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class AsyncLock : SemaphoreSlim
{
    public AsyncLock(ILogger? logger = null) : base(1, 1)
    {
        this.Logger = logger;
    }

    public ILogger? Logger { get; }
}

public static class AsyncLockExtension
{
    public static async Task<IDisposable> Enter(this AsyncLock semaphore, string? description = null)
    {
        var cleanedDescription = string.IsNullOrEmpty(description) ? string.Empty : " " + description;
        semaphore.Logger?.LogTrace("enter semaphore{Description}", cleanedDescription);

        var wrapper = new LockHolder(semaphore, cleanedDescription);
        await wrapper.Semaphore.WaitAsync();

        semaphore.Logger?.LogTrace("semaphore entered{Description}", cleanedDescription);

        return wrapper;
    }

    private sealed class LockHolder : IDisposable
    {
        private readonly string description;
        public AsyncLock Semaphore { get; }

        public LockHolder(AsyncLock semaphore, string description)
        {
            this.Semaphore = semaphore;
            this.description = description;
        }

        public void Dispose()
        {
            this.Semaphore.Logger?.LogTrace("leave semaphore{Description}", this.description);
            this.Semaphore.Release();
        }
    }
}