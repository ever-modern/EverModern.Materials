using System.Collections.Concurrent;

namespace EverModern.Threading.Locks;

/// <summary>
/// Provides thread-safe, key-level locked access to a dictionary.
/// Each key is locked independently, so different keys do not contend.
/// The lock is held for the lifetime of the returned <see cref="LockedDictionaryEntry{TKey,TValue}"/>.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary. Must not be null.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public class LockingDictionary<TKey, TValue>(
    IEqualityComparer<TKey> comparer
) : IDisposable where TKey : notnull
{
    readonly KeyLocker<TKey> _locker = new(comparer);
    readonly ConcurrentDictionary<TKey, TValue> _store = new(comparer);

    /// <summary>
    /// Initializes a new instance using the default equality comparer.
    /// </summary>
    public LockingDictionary() : this(EqualityComparer<TKey>.Default) { }

    /// <summary>
    /// Acquires an exclusive lock for <paramref name="key"/> and either retrieves
    /// an existing value or creates one using <paramref name="valueFactory"/>.
    /// The lock is released when the returned entry is disposed or removed.
    /// </summary>
    /// <param name="key">The key to lock and look up.</param>
    /// <param name="valueFactory">A factory that produces a value for the key if one does not exist.</param>
    /// <returns>A <see cref="LockedDictionaryEntry{TKey,TValue}"/> holding the key-level lock.</returns>
    public LockedDictionaryEntry<TKey, TValue> Acquire(TKey key, Func<TKey, TValue> valueFactory)
    {
        LockedScope? lockedKey = null;
        try
        {
            lockedKey = _locker.Lock(key);
            var value = _store.GetOrAdd(key, valueFactory);
            return new(
                _store,
                key,
                value,
                lockedKey
            );
        }
        catch
        {
            lockedKey?.Exit();
            throw;
        }
    }

    /// <summary>
    /// Disposes the underlying key locker. Does not affect already-acquired entries.
    /// </summary>
    public void Dispose()
    {
        _locker.Dispose();
    }
}

/// <summary>
/// Represents a key-value pair with an exclusive lock on the dictionary entry.
/// The lock is released when the entry is disposed or removed.
/// After disposal, properties and methods throw <see cref="ObjectDisposedException"/>.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
public class LockedDictionaryEntry<TKey, TValue>(
    ConcurrentDictionary<TKey, TValue> store,
    TKey key,
    TValue? dictValue,
    LockedScope locker
) : IDisposable where TKey : notnull
{
    readonly Lock _disposedLock = new();
    bool _disposed;

    T ThrowIfDisposed<T>(T returnedValue)
    {
        if (_disposed)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        return returnedValue;
    }

    /// <summary>
    /// Gets the key associated with this entry.
    /// </summary>
    public TKey Key => key;

    /// <summary>
    /// Gets or sets the value for this entry.
    /// Setting the value also updates the underlying dictionary.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the entry has been disposed.</exception>
    public TValue? Value
    {
        get => ThrowIfDisposed(dictValue);
        set
        {
            dictValue = ThrowIfDisposed(value);
            store[key] = value;
        }
    }

    /// <summary>
    /// Removes the entry from the underlying dictionary and releases the lock.
    /// Safe to call multiple times — subsequent calls are no-ops.
    /// </summary>
    public void Remove()
    {
        if (_disposedLock.TryEnter() == false)
            return;

        _disposed = true;
        store.Remove(key, out _);
        locker.Exit();
    }

    /// <summary>
    /// Releases the lock without removing the entry from the underlying dictionary.
    /// Safe to call multiple times — subsequent calls are no-ops.
    /// </summary>
    public void Dispose()
    {
        if (_disposedLock.TryEnter() == false)
            return;

        _disposed = true;
        locker.Exit();
    }
}
