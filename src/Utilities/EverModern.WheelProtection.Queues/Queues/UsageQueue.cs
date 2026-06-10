using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EverModern.Threading.Queues;

/// <summary>
/// Coordinates exclusive usage of items by returning disposable lockers.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public class UsageQueue<T> : IDisposable
    where T : class
{
    // Maps each actively locked item to its per-item semaphore (count = 0 while locked).
    readonly Dictionary<T, SemaphoreSlim> _slots = [];
    readonly Lock _lock = new();
    bool _disposed;

    /// <summary>
    /// Asynchronously acquires a locker for the specified item.
    /// </summary>
    /// <param name="item">The item to lock.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async ValueTask<ItemLocker<T>> OccupyAsync(
        T item,
        CancellationToken cancellationToken = default
    )
    {
        SemaphoreSlim semaphore;

        using (var _ = _lock.LockScope())
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_slots.TryGetValue(item, out semaphore))
            {
                // Item is free — register it as locked and return immediately.
                semaphore = new SemaphoreSlim(0, 1);
                _slots[item] = semaphore;
                return MakeLocker(item, semaphore);
            }
        }

        // Item is busy — wait outside the lock.
        await semaphore.WaitAsync(cancellationToken);

        // Re-acquire the lock to re-register ownership after being signalled.
        using (var _ = _lock.LockScope())
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return MakeLocker(item, semaphore);
        }
    }

    ItemLocker<T> MakeLocker(T item, SemaphoreSlim semaphore) =>
        new CallbackItemLocker<T>(
            item,
            _ =>
            {
                using var __ = _lock.LockScope();

                if (_disposed)
                {
                    return;
                }

                // If anyone is waiting on the semaphore, release one to wake the next waiter.
                // Otherwise remove the slot so the item is considered free again.
                if (semaphore.CurrentCount == 0 && _slots.ContainsKey(item))
                {
                    semaphore.Release();
                }

                _slots.Remove(item);
            }
        );

    /// <inheritdoc />
    public void Dispose()
    {
        using var _ = _lock.LockScope();

        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var semaphore in _slots.Values)
        {
            semaphore.Dispose();
        }

        _slots.Clear();
    }
}
