namespace EverModern.Threading.Queues;

/// <summary>
/// A disposable scope that holds a <see cref="Lock"/> for the duration of a synchronous block.
/// </summary>
public readonly struct LockedScope : IDisposable
{
    private readonly Lock _locker;

    /// <summary>
    /// Enters the lock and creates a scope that exits it on disposal.
    /// </summary>
    /// <param name="locker">The lock to enter.</param>
    public LockedScope(Lock locker)
    {
        _locker = locker;
        locker.Enter();
    }

    /// <inheritdoc/>
    public void Dispose() => _locker.Exit();
}

/// <summary>
/// A disposable scope that holds a <see cref="SemaphoreSlim"/> for the duration of a block.
/// Must be obtained via <see cref="AsyncLockedScope.EnterAsync"/> or <see cref="AsyncLockedScope.Enter"/>.
/// </summary>
public readonly struct AsyncLockedScope : IDisposable
{
    private readonly SemaphoreSlim _semaphore;

    private AsyncLockedScope(SemaphoreSlim semaphore) => _semaphore = semaphore;

    /// <summary>
    /// Synchronously acquires the semaphore and returns a scope that releases it on disposal.
    /// </summary>
    /// <param name="semaphore">The semaphore to acquire.</param>
    public static AsyncLockedScope Enter(SemaphoreSlim semaphore)
    {
        semaphore.Wait();
        return new(semaphore);
    }

    /// <summary>
    /// Asynchronously acquires the semaphore and returns a scope that releases it on disposal.
    /// </summary>
    /// <param name="semaphore">The semaphore to acquire.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async ValueTask<AsyncLockedScope> EnterAsync(
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken = default)
    {
        await semaphore.WaitAsync(cancellationToken);
        return new(semaphore);
    }

    /// <inheritdoc/>
    public void Dispose() => _semaphore.Release();
}
