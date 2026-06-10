namespace EverModern.Events;

public sealed class AsyncEventSource : IAsyncNotifier, IDisposable
{
    private readonly Lock _lock = new();

    private Func<ValueTask>? _handlers;
    private int _disposed;

    public Subscription Subscribe(Func<ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_lock)
        {
            ThrowIfDisposed();

            _handlers += handler;
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                if (_disposed != 0)
                    return;

                _handlers -= handler;
            }
        });
    }

    public async ValueTask InvokeAsync()
    {
        Func<ValueTask>? snapshot;

        lock (_lock)
        {
            ThrowIfDisposed();

            snapshot = _handlers;
        }

        if (snapshot is null)
            return;

        // multicast delegate invocation
        foreach (Func<ValueTask> handler in snapshot.GetInvocationList())
        {
            await handler().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            _handlers = null;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed != 0)
            throw new ObjectDisposedException(nameof(AsyncEventSource));
    }
}

public sealed class AsyncEventSource<T> : IAsyncNotifier<T>, IDisposable
{
    private readonly Lock _lock = new();

    private Func<T, ValueTask>? _handlers;
    private int _disposed;

    public Subscription Subscribe(Func<T, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_lock)
        {
            ThrowIfDisposed();

            _handlers += handler;
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                if (_disposed != 0)
                    return;

                _handlers -= handler;
            }
        });
    }

    public async ValueTask InvokeAsync(T value)
    {
        Func<T, ValueTask>? snapshot;

        lock (_lock)
        {
            ThrowIfDisposed();

            snapshot = _handlers;
        }

        if (snapshot is null)
            return;

        foreach (Func<T, ValueTask> handler in snapshot.GetInvocationList())
        {
            await handler(value).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            _handlers = null;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed != 0)
            throw new ObjectDisposedException(nameof(AsyncEventSource<T>));
    }
}
