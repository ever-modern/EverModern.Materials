using System.Collections.Concurrent;

namespace EverModern.Threading.Locks;

public class KeyLocker<TKey> : IDisposable
    where TKey : notnull
{
    readonly ConcurrentDictionary<TKey, LockEntry> _locks;
    int _disposed;

    public KeyLocker()
        : this(EqualityComparer<TKey>.Default) { }

    public KeyLocker(IEqualityComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        _locks = new ConcurrentDictionary<TKey, LockEntry>(comparer);
    }

    public LockedScope Lock(TKey key)
    {
        ThrowIfDisposed();

        var entry = _locks.GetOrAdd(key, _ => new LockEntry());

        Interlocked.Increment(ref entry.RefCount);

        try
        {
            entry.Lock.Enter();

            var scope = LockedScope.Enter(entry.Lock);

            scope.BeforeExit.Subscribe(_ =>
            {
                entry.Lock.Exit();
                ReleaseReference(key, entry);
            });

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
            {
                // Lock does not require disposal
                // but entry may still be removed safely
            }
        }

        _locks.Clear();
    }

    void ReleaseReference(TKey key, LockEntry entry)
    {
        if (Interlocked.Decrement(ref entry.RefCount) != 0)
            return;

        if (_locks.TryGetValue(key, out var current) &&
            ReferenceEquals(current, entry) &&
            _locks.TryRemove(new KeyValuePair<TKey, LockEntry>(key, entry)))
        {
            // nothing to dispose for Lock
        }
    }

    void ThrowIfDisposed()
    {
        if (_disposed != 0)
            throw new ObjectDisposedException(nameof(KeyLocker<TKey>));
    }

    sealed class LockEntry
    {
        public readonly Lock Lock = new();
        public int RefCount;
    }
}
