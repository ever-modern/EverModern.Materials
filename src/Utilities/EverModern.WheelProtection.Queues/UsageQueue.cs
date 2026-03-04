using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EverModern.WheelProtection.Queues;

/// <summary>
/// Coordinates exclusive usage of items by returning disposable lockers.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public class UsageQueue<T> : IDisposable 
    where T : class
{
    readonly List<T> _items = [];
    readonly List<(T, TaskCompletionSource<ItemLocker<T>>)> _requests = [];
    bool _disposed;

    void CheckDisposed()
    {
        lock (this)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (this)
        {
            if (_disposed is false)
            {
                var disposedException = new ObjectDisposedException(
                    GetType().FullName,
                    "Usage queue has been disposed. All requests for items locking are dropped.");

                _requests.ForEach(r => r.Item2.TrySetException(disposedException));
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Asynchronously acquires a locker for the specified item.
    /// </summary>
    /// <param name="item">The item to lock.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public Task<ItemLocker<T>> OccupyAsync(T item, CancellationToken cancellationToken)
    {
        CheckDisposed();
        lock (_items)
        {
            var l = _items.Count;
            for (int i = 0; i < l; i++)
            {
                var item2 = _items[i];
                if (ReferenceEquals(item2, item))
                {
                    TaskCompletionSource<ItemLocker<T>> request = new();
                    _requests.Add((item, request));

                    cancellationToken.Register(() =>
                    {
                        lock (_items)
                        {
                            var requestsCount = _requests.Count;
                            for (int j = 0; j < requestsCount; j++)
                            {
                                var iRequest = _requests[j];
                                if (ReferenceEquals(request, iRequest.Item2))
                                {
                                    _requests.RemoveAt(j);
                                    request.SetException(new TaskCanceledException());
                                }
                            }

                        }
                    });

                    return request.Task;

                }
            }

            _items.Add(item);

            var result = LockItem(item);

            return Task.FromResult(result);
        }
    }

    ItemLocker<T> LockItem(T item)
        => new CallbackItemLocker<T>(item, (_) =>
        {
            lock (_items)
            {
                _items.Remove(item);
                var l = _requests.Count;
                for (int i = 0; i < l; i++)
                {
                    var (requestedItem, task) = _requests[i];
                    if (ReferenceEquals(requestedItem, item))
                    {
                        _requests.RemoveAt(i);
                        task.SetResult(LockItem(item));
                        return;
                    }
                }
            }
        });
}
