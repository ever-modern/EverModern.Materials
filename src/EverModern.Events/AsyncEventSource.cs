namespace EverModern.Events;

public sealed class AsyncEventSource : BaseAsyncEventSource<Nothing>, IAsyncNotifier
{
    public ValueTask InvokeAsync() => base.InvokeAsync(default);

    public Subscription Subscribe(Func<ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return base.Subscribe(_ => handler());
    }
}

public sealed class AsyncEventSource<T> : BaseAsyncEventSource<T>, IAsyncNotifier<T>
{
    public ValueTask InvokeAsync(T value) => base.InvokeAsync(value);

    public Subscription Subscribe(Func<T, ValueTask> handler)
        => base.Subscribe(handler);
}

public abstract class BaseAsyncEventSource<T> : IDisposable
{
    readonly object _sync = new();

    Func<T, ValueTask>? _handlers;
    bool _disposed;

    protected Subscription Subscribe(Func<T, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_sync)
        {
            ThrowIfDisposed();
            _handlers += handler;
        }

        return new Subscription(() =>
            {
                lock (_sync)
                {
                    if (_disposed)
                        return;

                    _handlers -= handler;
                }
            }
        );
    }

    protected async ValueTask InvokeAsync(T value)
    {
        Func<T, ValueTask>? snapshot;

        lock (_sync)
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
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
            _handlers = null;
        }
    }

    void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
    }
}
