using System.Collections.Concurrent;

namespace EverModern.Threading.Locks;

public sealed class AsyncKeyLocker<TKey> : IDisposable
    where TKey : notnull
{
    readonly ConcurrentDictionary<TKey, LockEntry> _locks;
    int _disposed;

    public AsyncKeyLocker()
        : this(EqualityComparer<TKey>.Default) {}

    public AsyncKeyLocker(IEqualityComparer<TKey> comparer) { _locks = new ConcurrentDictionary<TKey, LockEntry>(comparer); }

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
            ReferenceEquals(current, entry) &&
            _locks.TryRemove(key, out _))
        {
            if (_disposed != 0)
                entry.Semaphore.Dispose();
        }
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
