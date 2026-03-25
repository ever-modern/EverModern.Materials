using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EverModern.WheelProtection.Queues;

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
/// <remarks>
/// Initializes a new recycler with the specified pool size.
/// </remarks>
/// <param name="maxPoolSize">The maximum pool size.</param>
public abstract class Recycler<T>(int maxPoolSize) : IDisposable
    where T : class
{
    readonly object _locker = new();
    volatile bool _isDisposed;
    /// <summary>
    /// Tries to construct a new item if possible.
    /// </summary>
    /// <param name="item">The created item.</param>
    /// <returns>
    /// <see langword="true"/> if an item was created; otherwise <see langword="false"/>.
    /// </returns>
    protected abstract bool TryCreateNew(out T item);

    /// <summary>
    /// Determines whether the item can be returned to the pool.
    /// </summary>
    /// <param name="item">The item to validate.</param>
    /// <returns><see langword="true"/> if the item can be reused.</returns>
    protected abstract bool IsWell(T item);

    /// <summary>
    /// Disposes of an item that is not reusable.
    /// </summary>
    /// <param name="item">The item to discard.</param>
    protected abstract void Discard(T item);

    readonly ItemManager[] _pool = new ItemManager[maxPoolSize];
    readonly Queue<TaskCompletionSource<ItemLocker<T>>> _subscriptions
        = [];

    /// <summary>
    /// Requests a new item locker, waiting for availability if needed.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The item locker.</returns>
    /// <exception cref="InvalidOperationException">No items can be created and none are available.</exception>
    public ValueTask<ItemLocker<T>> Another(CancellationToken cancellationToken = default)
    {
        lock (_locker)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            var spanPool = _pool.AsSpan();
            int i = 0;
            int nonEmptyCount = 0;
            for (; i < spanPool.Length; i++)
            {
                var stateItem = spanPool[i];
                if (stateItem is null)
                {
                    continue;
                }
                
                nonEmptyCount++;

                if (stateItem.Available == true)
                {
                    stateItem.Available = false;

                    return new ValueTask<ItemLocker<T>>(stateItem);
                }
            }
            if (nonEmptyCount == _pool.Length)
            {
                return WaitOnPooledItem(cancellationToken);
            }
            for (i = 0; i < spanPool.Length; i++)
            {
                if (spanPool[i] is null)
                {
                    ItemManager itemManager = CreateNewManager(i);

                    if (itemManager is null)
                    {
                        if (nonEmptyCount == 0)
                        {
                            throw new InvalidOperationException("No items in the pool and new item was not retrieved. Request can't be carried out.");
                        }

                        return WaitOnPooledItem(cancellationToken);
                    }

                    return new ValueTask<ItemLocker<T>>(itemManager);
                }
            }
            throw new InvalidOperationException();
        }
    }

    ValueTask<ItemLocker<T>> WaitOnPooledItem(CancellationToken cancellationToken)
    {
        var taskSource = new TaskCompletionSource<ItemLocker<T>>();
        _subscriptions.Enqueue(taskSource);
        cancellationToken.Register(() => taskSource.TrySetCanceled());

        return new ValueTask<ItemLocker<T>>(taskSource.Task);
    }

    ItemManager CreateNewManager(int i)
    {
        if (!TryCreateNew(out var item))
        {
            return null;
        }
        var itemManager = new ItemManager(item, OnItemReleased(i, item));
        _pool[i] = itemManager;
        return itemManager;
    }

    Action<CallbackItemLocker<T>> OnItemReleased(int i, T item)
        => im =>
        {
            lock (_locker)
            {
                if (IsWell(item))
                {
                    while (_subscriptions.TryDequeue(out var request))
                    {
                        if (request.Task.IsCompleted)
                        {
                            continue;
                        }
                        if (request.TrySetResult(im))
                        {
                            return;
                        }
                    }
                    ((ItemManager)im).Available = true;
                }
                else
                {
                    Discard(item);
                    im = null;
                    while (_subscriptions.TryDequeue(out var request))
                    {
                        if (request.Task.IsCompleted)
                        {
                            continue;
                        }
                        im = CreateNewManager(i);
                        if (request.TrySetResult(im))
                        {
                            return;
                        }
                    }
                    if (im is null)
                    {
                        _pool[i] = null;
                    }
                }
            }
        };

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        foreach (var item in _pool)
        {
            item.Dispose();
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
