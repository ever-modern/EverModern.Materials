namespace EverModern.DataProvision;

/// <summary>
/// Represents a subscription handle that can release a callback registration.
/// </summary>
public class SubscriptionHolder : IDisposable
{
    /// <inheritdoc />
    void IDisposable.Dispose()
        => Release();

    readonly Action _onReleased;

    internal SubscriptionHolder(Action onReleased)
    {
        _onReleased = onReleased;
    }

    /// <summary>
    /// Releases the subscription.
    /// </summary>
    public void Release() => _onReleased();
}
