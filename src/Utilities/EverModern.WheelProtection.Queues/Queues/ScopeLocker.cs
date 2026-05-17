namespace EverModern.Threading.Queues;

/// <summary>
/// A disposable scope that holds a <see cref="Lock"/> for the duration of a synchronous block.
/// </summary>
public readonly struct ScopeLocker : IDisposable
{
    private readonly Lock _locker;

    /// <summary>
    /// Enters the lock and creates a scope that exits it on disposal.
    /// </summary>
    /// <param name="locker">The lock to enter.</param>
    public ScopeLocker(Lock locker)
    {
        _locker = locker;
        locker.Enter();
    }

    /// <inheritdoc/>
    public void Dispose() => _locker.Exit();
}

/// <summary>
/// A disposable scope that holds a <see cref="SemaphoreSlim"/> for the duration of a block.
/// Must be obtained via <see cref="AsyncScopeLocker.EnterAsync"/> or <see cref="AsyncScopeLocker.Enter"/>.
/// </summary>
public readonly struct AsyncScopeLocker : IDisposable
{
    private readonly SemaphoreSlim _semaphore;

    private AsyncScopeLocker(SemaphoreSlim semaphore) => _semaphore = semaphore;

    /// <summary>
    /// Synchronously acquires the semaphore and returns a scope that releases it on disposal.
    /// </summary>
    /// <param name="semaphore">The semaphore to acquire.</param>
    public static AsyncScopeLocker Enter(SemaphoreSlim semaphore)
    {
        semaphore.Wait();
        return new(semaphore);
    }

    /// <summary>
    /// Asynchronously acquires the semaphore and returns a scope that releases it on disposal.
    /// </summary>
    /// <param name="semaphore">The semaphore to acquire.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async ValueTask<AsyncScopeLocker> EnterAsync(
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken = default
    )
    {
        await semaphore.WaitAsync(cancellationToken);
        return new(semaphore);
    }

    /// <inheritdoc/>
    public void Dispose() => _semaphore.Release();
}
