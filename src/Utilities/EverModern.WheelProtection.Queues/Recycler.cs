using System;
using System.Collections.Concurrent;
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
        var index = Interlocked.Increment(ref _reachedItem) - 1;
        if (index >= _fixedPool.Count)
        {
            item = default;
            return false;
        }
        item = _fixedPool[index];
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
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentQueue<ItemManager> _available = new();
    private readonly int _maxPoolSize;
    private int _isDisposed;
    private int _createdCount;
    private readonly CancellationTokenSource _disposedCts = new();

    protected Recycler(int maxPoolSize)
    {
        _maxPoolSize = maxPoolSize;
        _semaphore = new SemaphoreSlim(maxPoolSize, maxPoolSize);
    }

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

    /// <summary>
    /// Requests a new item locker, waiting for availability if needed.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The item locker.</returns>
    /// <exception cref="InvalidOperationException">No items can be created and none are available.</exception>
    public ValueTask<ItemLocker<T>> Another(CancellationToken cancellationToken = default)
    {
        if (_available.TryDequeue(out var itemManager))
        {
            return new ValueTask<ItemLocker<T>>(itemManager);
        }

        return SlowPath(cancellationToken);
    }

    private async ValueTask<ItemLocker<T>> SlowPath(CancellationToken cancellationToken)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposedCts.Token);
        try
        {
            await _semaphore.WaitAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (_disposedCts.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(Recycler<T>));
            throw;
        }
        finally
        {
            linkedCts.Dispose();
        }

        if (_isDisposed != 0)
        {
            _semaphore.Release();
            throw new ObjectDisposedException(nameof(Recycler<T>));
        }

        if (_available.TryDequeue(out var itemManager))
        {
            return itemManager;
        }

        var newCount = Interlocked.Increment(ref _createdCount);
        if (newCount > _maxPoolSize)
        {
            Interlocked.Decrement(ref _createdCount);
            _semaphore.Release();
            throw new InvalidOperationException("Pool size exceeded unexpectedly.");
        }

        if (TryCreateNew(out var item))
        {
            return new ItemManager(item, OnItemReleased());
        }
        else
        {
            Interlocked.Decrement(ref _createdCount);
            _semaphore.Release();
            throw new InvalidOperationException("No items in the pool...");
        }
    }
    private Action<CallbackItemLocker<T>> OnItemReleased()
        => im =>
        {
            var itemManager = (ItemManager)im;
            if (_isDisposed != 0)
            {
                itemManager.Dispose();
                return;
            }
            if (IsWell(itemManager.Item))
            {
                _available.Enqueue(itemManager);
            }
            else
            {
                Discard(itemManager.Item);
                Interlocked.Decrement(ref _createdCount);
            }
            _semaphore.Release();
        };

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            return;
        _disposedCts.Cancel();
        while (_available.TryDequeue(out var item))
        {
            item.Dispose();
        }
        // Do not dispose _semaphore here; let outstanding operations finish safely.
        // _semaphore.Dispose(); // Only if you can guarantee no more usage.
        _disposedCts.Dispose();
    }

    sealed class ItemManager : CallbackItemLocker<T>
    {
        public ItemManager(T item, Action<CallbackItemLocker<T>> onDisposed)
            : base(item, onDisposed)
        {
        }
        public T Item => base.Item;
    }
}
