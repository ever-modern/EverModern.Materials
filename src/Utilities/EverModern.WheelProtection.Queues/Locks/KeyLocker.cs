using System.Collections.Concurrent;

namespace EverModern.Threading.Locks;

/// <summary>
/// Provides synchronous, key-level mutual exclusion using <see cref="Lock"/> per key.
/// Different keys can be locked concurrently without contention.
/// Disposing the locker prevents new acquisitions.
/// </summary>
/// <typeparam name="TKey">The type of keys. Must not be null.</typeparam>
public class KeyLocker<TKey> : IDisposable
    where TKey : notnull
{
    readonly ConcurrentDictionary<TKey, LockEntry> _locks;
    int _disposed;

    /// <summary>
    /// Initializes a new instance using the default equality comparer.
    /// </summary>
    public KeyLocker()
        : this(EqualityComparer<TKey>.Default) { }

    /// <summary>
    /// Initializes a new instance with the specified key comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer for keys.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="comparer"/> is null.</exception>
    public KeyLocker(IEqualityComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        _locks = new ConcurrentDictionary<TKey, LockEntry>(comparer);
    }

    /// <summary>
    /// Synchronously acquires an exclusive lock for the specified key.
    /// The lock is released when the returned <see cref="LockedScope"/> is disposed.
    /// </summary>
    /// <param name="key">The key to lock.</param>
    /// <returns>A <see cref="LockedScope"/> that releases the lock on disposal.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the locker has been disposed.</exception>
    public LockedScope Lock(TKey key)
    {
        ThrowIfDisposed();

        var entry = _locks.GetOrAdd(key, _ => new LockEntry());

        Interlocked.Increment(ref entry.RefCount);

        try
        {
            var scope = LockedScope.Enter(entry.Lock);

            scope.BeforeExit.Subscribe(_ =>
                ReleaseReference(key, entry)
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
    /// Active lock scopes continue to function normally.
    /// </summary>
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
