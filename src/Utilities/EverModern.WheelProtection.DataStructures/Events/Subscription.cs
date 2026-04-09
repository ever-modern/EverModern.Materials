namespace EverModern.WheelProtection.DataStructures.Events;

public sealed class Subscription(Action<Subscription> onDisposed) : IDisposable
{
    private Action<Subscription>? _onDisposed = onDisposed;

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
