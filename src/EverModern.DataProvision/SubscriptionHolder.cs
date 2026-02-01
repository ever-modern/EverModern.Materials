namespace EverModern.DataProvision;

public class SubscriptionHolder : IDisposable
{
    void IDisposable.Dispose()
        => Release();

    readonly Action _onReleased;

    internal SubscriptionHolder(Action onReleased)
    {
        _onReleased = onReleased;
    }

    public void Release() => _onReleased();
}
