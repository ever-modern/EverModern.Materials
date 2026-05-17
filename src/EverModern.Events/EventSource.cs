namespace EverModern.Events;

public sealed class EventSource<T> : INotifier<T>, IDisposable
{
    private readonly object _lock = new();
    private Action<T>? _handlers;

    public Subscription Subscribe(Action<T> handler)
    {
        lock (_lock)
        {
            _handlers += handler;
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                _handlers -= handler;
            }
        });
    }

    public void Invoke(T value)
    {
        Action<T>? handlers;

        lock (_lock)
        {
            handlers = _handlers;
        }

        handlers?.Invoke(value);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _handlers = null;
        }
    }
}

public sealed class EventSource : INotifier, IDisposable
{
    private readonly object _lock = new();
    private Action? _handlers;

    public Subscription Subscribe(Action handler)
    {
        lock (_lock)
        {
            _handlers += handler;
        }
        return new Subscription(() =>
        {
            lock (_lock)
            {
                _handlers -= handler;
            }
        });
    }

    public void Invoke()
    {
        Action? handlers;
        lock (_lock)
        {
            handlers = _handlers;
        }
        handlers?.Invoke();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _handlers = null;
        }
    }
}
