using System.Collections.Concurrent;
using EverModern.Events;

namespace EverModern.Threading;

/// <summary>
/// Provides a per-key asynchronous locking mechanism where each key
/// is associated with a <see cref="SemaphoreSlim"/>.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify locks.</typeparam>
/// <remarks>
/// Each key maps to a single semaphore. When all scopes for a key are released,
/// the semaphore is removed from the internal dictionary.
/// </remarks>
public sealed class KeyLocker<TKey> : IDisposable
{
    private readonly ConcurrentDictionary<TKey, SemaphoreSlim> _locks;

    private int _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyLocker{TKey}"/> class
    /// using the default equality comparer for <typeparamref name="TKey"/>.
    /// </summary>
    public KeyLocker()
        : this(EqualityComparer<TKey>.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyLocker{TKey}"/> class
    /// using the specified equality comparer.
    /// </summary>
    /// <param name="comparer">
    /// The equality comparer used to compare keys.
    /// </param>
    public KeyLocker(IEqualityComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        _locks = new ConcurrentDictionary<TKey, SemaphoreSlim>(comparer);
    }

    /// <summary>
    /// Asynchronously acquires an exclusive lock for the specified key.
    /// </summary>
    /// <param name="key">The key to lock.</param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the wait operation.
    /// </param>
    /// <returns>
    /// A <see cref="LockedScope"/> that releases the lock when disposed.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the <see cref="KeyLocker{TKey}"/> has been disposed.
    /// </exception>
    public async Task<LockedScope> LockAsync(
        TKey key,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        return new KeyLockedScope(this, key, semaphore);
    }

    /// <summary>
    /// Releases all internal semaphores and prevents further usage.
    /// </summary>
    /// <remarks>
    /// Existing scopes will still complete correctly, but no new locks can be acquired.
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _locks.Clear();
    }

    /// <summary>
    /// Releases a semaphore associated with a specific key.
    /// </summary>
    private void Release(TKey key, SemaphoreSlim semaphore)
    {
        semaphore.Release();

        // Best-effort cleanup for idle semaphores.
        // Only remove the mapping when it still points to the same instance.
        if (semaphore.CurrentCount == 1
            && _locks.TryGetValue(key, out var current)
            && ReferenceEquals(current, semaphore))
        {
            _locks.TryRemove(key, out _);

            if (_disposed != 0)
            {
                semaphore.Dispose();
            }
        }
    }

    /// <summary>
    /// Throws if this instance has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed != 0)
            throw new ObjectDisposedException(nameof(KeyLocker<TKey>));
    }

    /// <summary>
    /// Represents a scoped lock acquired for a specific key.
    /// </summary>
    private sealed class KeyLockedScope : LockedScope
    {
        private readonly KeyLocker<TKey> _owner;
        private readonly TKey _key;
        private readonly SemaphoreSlim _semaphore;

        private int _released;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyLockedScope"/> class.
        /// </summary>
        public KeyLockedScope(
            KeyLocker<TKey> owner,
            TKey key,
            SemaphoreSlim semaphore)
        {
            _owner = owner;
            _key = key;
            _semaphore = semaphore;
        }

        /// <summary>
        /// Called when the scope exits. Releases the underlying semaphore.
        /// </summary>
        protected override void OnExit()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0)
                return;

            _owner.Release(_key, _semaphore);
        }
    }
}