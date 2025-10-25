namespace DestallMaterials.EnlightenedDataProvision;

public class SubscriptionHolder : IDisposable
{
    void IDisposable.Cancel()
        => Release();

    readonly Action _onReleased;

    internal SubscriptionHolder(Action onReleased)
    {
        _onReleased = onReleased;
    }

    public void Release() => _onReleased();
}
