using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EverModern.Threading.Locks;

namespace EverModern.Threading.Queues;

/// <summary>
/// Works on a fixed pool of items. Creating new items will not happen.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public abstract class FixedPoolRecycler<T> : Recycler<T> where T : class
{
    readonly IReadOnlyList<T> _fixedPool;
    int _reachedItem;

    /// <summary>
    /// Initializes a new instance with the fixed pool items.
    /// </summary>
    /// <param name="items">The fixed pool items.</param>
    protected FixedPoolRecycler(IReadOnlyList<T> items)
        : base(items.Count)
    {
        _fixedPool = items;
    }

    protected override sealed bool TryCreateNew(out T item)
    {
        if (_reachedItem == _fixedPool.Count)
        {
            item = default;
            return false;
        }

        item = _fixedPool[_reachedItem++];
        return true;
    }
}

/// <summary>
/// Accumulates pool of items up to the limit provided.
/// Awaits items to be released from use and yields them once it's done.
/// Creates new items when other items in the pool are busy and there is still room in the pool.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public abstract class Recycler<T> : IDisposable
    where T : class
{
    // Gate: limits concurrent callers to the pool size. Each released item does a Release().
    readonly SemaphoreSlim _gate;
    readonly Lock _lock = new();
    readonly ItemManager?[] _pool;
    bool _isDisposed;

    /// <summary>
    /// Tries to construct a new item if possible.
    /// </summary>
    protected abstract bool TryCreateNew(out T item);

    /// <summary>
    /// Determines whether the item can be returned to the pool.
    /// </summary>
    protected abstract bool IsWell(T item);

    /// <summary>
    /// Disposes of an item that is not reusable.
    /// </summary>
    protected abstract void Discard(T item);

    /// <summary>
    /// Initializes a new recycler with the specified pool size.
    /// </summary>
    /// <param name="maxPoolSize">The maximum pool size.</param>
    protected Recycler(int maxPoolSize)
    {
        _pool = new ItemManager[maxPoolSize];
        // Start with full capacity — each slot is a potential permit.
        _gate = new SemaphoreSlim(maxPoolSize, maxPoolSize);
    }

    /// <summary>
    /// Requests a new item locker, waiting for availability if needed.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The item locker.</returns>
    /// <exception cref="InvalidOperationException">No items can be created and none are available.</exception>
    public async ValueTask<ItemLocker<T>> Another(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        // Wait until at least one slot is free (either available or creatable).
        await _gate.WaitAsync(cancellationToken);

        using var _ = _lock.LockScope();

        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var spanPool = _pool.AsSpan();

        // 1. Try to find an available (idle) item.
        for (int i = 0; i < spanPool.Length; i++)
        {
            var manager = spanPool[i];
            if (manager is { Available: true })
            {
                manager.Available = false;
                return manager;
            }
        }

        // 2. No idle item — find an empty slot and create a new one.
        for (int i = 0; i < spanPool.Length; i++)
        {
            if (spanPool[i] is null)
            {
                var manager = CreateNewManager(i);
                if (manager is not null)
                {
                    return manager;
                }

                // TryCreateNew returned false; nothing we can do.
                throw new InvalidOperationException(
                    "No items in the pool and a new item could not be created. Request can't be carried out.");
            }
        }

        throw new InvalidOperationException("Recycler is in an inconsistent state.");
    }

    ItemManager CreateNewManager(int slotIndex)
    {
        if (!TryCreateNew(out var item))
        {
            return null;
        }

        var manager = new ItemManager(item, OnItemReleased(slotIndex, item));
        _pool[slotIndex] = manager;
        return manager;
    }

    Action<CallbackItemLocker<T>> OnItemReleased(int slotIndex, T item)
        => im =>
        {
            using var _ = _lock.LockScope();

            if (IsWell(item))
            {
                ((ItemManager)im).Available = true;
            }
            else
            {
                Discard(item);
                _pool[slotIndex] = null;

                // Try to eagerly fill the slot so the next waiter gets a fresh item.
                _pool[slotIndex] = CreateNewManager(slotIndex);
            }

            // Release one permit so the next waiter in WaitAsync can proceed.
            _gate.Release();
        };

    /// <inheritdoc />
    public void Dispose()
    {
        using var _ = _lock.LockScope();

        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _gate.Dispose();

        foreach (var item in _pool)
        {
            item?.Dispose();
        }
    }

    sealed class ItemManager : CallbackItemLocker<T>
    {
        public ItemManager(T item, Action<CallbackItemLocker<T>> onDisposed)
            : base(item, onDisposed)
        {
        }

        public bool Available { get; set; }
    }
}
