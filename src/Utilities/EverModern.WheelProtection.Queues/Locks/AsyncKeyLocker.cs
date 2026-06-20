using System.Collections.Concurrent;

namespace EverModern.Threading.Locks;

/// <summary>
/// Provides asynchronous, key-level mutual exclusion using <see cref="SemaphoreSlim"/> per key.
/// Different keys can be locked concurrently without contention.
/// Disposing the locker prevents new acquisitions and cleans up idle semaphores.
/// </summary>
/// <typeparam name="TKey">The type of keys. Must not be null.</typeparam>
public sealed class AsyncKeyLocker<TKey> : IDisposable
    where TKey : notnull
{
    readonly ConcurrentDictionary<TKey, LockEntry> _locks;
    int _disposed;

    /// <summary>
    /// Initializes a new instance using the default equality comparer.
    /// </summary>
    public AsyncKeyLocker()
        : this(EqualityComparer<TKey>.Default) { }

    /// <summary>
    /// Initializes a new instance with the specified key comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer for keys.</param>
    public AsyncKeyLocker(IEqualityComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        _locks = new ConcurrentDictionary<TKey, LockEntry>(comparer);
    }

    /// <summary>
    /// Asynchronously acquires an exclusive lock for the specified key.
    /// The lock is released when the returned <see cref="LockedScope"/> is disposed.
    /// </summary>
    /// <param name="key">The key to lock.</param>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    /// <returns>A <see cref="LockedScope"/> that releases the lock on disposal.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the locker has been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown if cancellation is requested.</exception>
    public async Task<LockedScope> LockAsync(
        TKey key,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var entry = _locks.GetOrAdd(key, _ => new LockEntry());

        Interlocked.Increment(ref entry.RefCount);

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            var scope = new LockedScope();
            scope.EnterScope();

            // Release happens strictly on scope exit
            scope.BeforeExit.Subscribe(_ =>
                {
                    entry.Semaphore.Release();
                    ReleaseReference(key, entry);
                }
            );

            return scope;
        }
        catch
        {
            ReleaseReference(key, entry);
            throw;
        }
    }

    /// <summary>
    /// Disposes the locker. Prevents new lock acquisitions.
    /// Active lock scopes continue to function; their semaphores are
    /// released when all references are gone.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        foreach (var (_, entry) in _locks)
        {
            if (Volatile.Read(ref entry.RefCount) == 0)
                entry.Semaphore.Dispose();
        }

        _locks.Clear();
    }

    void ReleaseReference(TKey key, LockEntry entry)
    {
        if (Interlocked.Decrement(ref entry.RefCount) != 0)
            return;

        if (_locks.TryGetValue(key, out var current) &&
            ReferenceEquals(current, entry))
        {
            _locks.TryRemove(key, out _);
        }

        if (_disposed != 0)
            entry.Semaphore.Dispose();
    }

    void ThrowIfDisposed()
    {
        if (_disposed != 0)
            throw new ObjectDisposedException(nameof(AsyncKeyLocker<TKey>));
    }

    sealed class LockEntry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int RefCount;
    }
}
