namespace EverModern.Events;

public readonly struct Nothing();

/// <summary>
/// Represents a thread-safe synchronous event source with subscription support.
/// </summary>
public class EventSource : BaseEventSource<Nothing>, INotifier
{
    public void Invoke() => base.Invoke(default);
    public Subscription Subscribe(Action handler) => Subscribe(_ => handler());
}

public class EventSource<T> : BaseEventSource<T>, INotifier<T>
{
    public void Invoke(T value) => base.Invoke(value);
    public Subscription Subscribe(Action<T> handler) => base.Subscribe(handler);
}

public abstract class BaseEventSource<T> : IDisposable
{
    readonly object _sync = new();

    Action<T>? _handlers;
    bool _disposed;

    protected Subscription Subscribe(Action<T> handler)
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

    protected void Invoke(T value)
    {
        Action<T>? snapshot;

        lock (_sync)
        {
            ThrowIfDisposed();
            snapshot = _handlers;
        }

        snapshot?.Invoke(value);
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
            throw new ObjectDisposedException(this.GetType().FullName);
    }
}
