using System;
using System.Threading;
using System.Threading.Tasks;

namespace DestallMaterials.WheelProtection.Queues;

public abstract class ContinuationToken : IDisposable
{
    public abstract void Cancel();
}

public interface ICompletionRateController
{
    ValueTask<ContinuationToken> WhenAllowed(CancellationToken cancellationToken);
}