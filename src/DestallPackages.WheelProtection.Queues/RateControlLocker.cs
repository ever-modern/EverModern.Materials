using System;

namespace DestallMaterials.WheelProtection.Queues;

public class RateControlLocker : ContinuationToken, IDisposable
{
    private readonly Action _onDisposed;

    public RateControlLocker(Action onDisposed)
    {
        _onDisposed = onDisposed;
    }

    public override void Dispose() => _onDisposed();
}
