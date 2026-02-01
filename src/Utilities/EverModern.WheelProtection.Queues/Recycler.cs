using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EverModern.WheelProtection.Queues;

/// <summary>
/// Works on a fixed pool of items. Creating new items will not happen.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class FixedPoolRecycler<T> : Recycler<T> where T : class
{
    readonly IReadOnlyList<T> _fixedPool;
    int _reachedItem;
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
/// Creates new items, when other items in the pool are busy and there is still room in the pool.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class Recycler<T> : IDisposable
    where T : class
{
    volatile bool _isDisposed;
    /// <summary>
    /// Try to construct new produced item directly, if possible.
    /// </summary>
    /// <param name="item">Created item</param>
    /// <returns>
    /// True - item has been created. It will be used later.
    /// False - item is not allowed to be created. Recycler will await other pooled items to be released.
    /// </returns>
    protected abstract bool TryCreateNew(out T item);

    /// <summary>
    /// Determine if item can be returned to pool with its instant state.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    protected abstract bool IsWell(T item);

    /// <summary>
    /// How to dispose of item, if its state is invalid, i.e. IsWell is false.
    /// </summary>
    /// <param name="item"></param>
    protected abstract void Discard(T item);

    readonly ItemManager[] _pool;

    protected Recycler(int maxPoolSize)
    {
        _pool = new ItemManager[maxPoolSize];
    }

    readonly Queue<TaskCompletionSource<ItemLocker<T>>> _subscriptions
        = [];

    /// <summary>
    /// Request new item locker from the Recycler, waiting for once it's:
    /// 1) created anew
    /// 2) released from usage
    /// 3) just taken out free from pool
    /// </summary>
    /// <returns>Tool releasing item for usage in another request.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public ValueTask<ItemLocker<T>> Another(CancellationToken cancellationToken = default)
    {
        lock (this)
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
            lock (this)
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
