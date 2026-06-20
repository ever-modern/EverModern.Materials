namespace EverModern.Events;

/// <summary>
/// Represents a disposable subscription handle.
/// </summary>
/// <remarks>
/// Disposing this instance unregisters the associated handler.
/// </remarks>
public sealed class Subscription(Action<Subscription> onDisposed) : IDisposable
{
    Action<Subscription>? _onDisposed = onDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Subscription"/> class.
    /// </summary>
    /// <param name="onDisposed">Callback invoked when the subscription is disposed.</param>
    public Subscription(Action onDisposed)
        : this(_ => onDisposed()) { }

    /// <summary>
    /// Disposes this subscription and unregisters the associated handler.
    /// </summary>
    public void Dispose()
    {
        var action = _onDisposed;
        if (action == null)
            return;

        _onDisposed = null;
        action(this);
    }
}
