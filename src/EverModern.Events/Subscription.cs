namespace EverModern.Events;

public sealed class Subscription(Action<Subscription> onDisposed) : IDisposable
{
    Action<Subscription>? _onDisposed = onDisposed;

    public Subscription(Action onDisposed)
        : this(_ => onDisposed()) { }

    public void Dispose()
    {
        var action = _onDisposed;
        if (action == null)
            return;

        _onDisposed = null;
        action(this);
    }
}
