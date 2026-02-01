using System;
using System.Threading;
using System.Threading.Tasks;

namespace EverModern.WheelProtection.Queues;

public abstract class ContinuationToken : IDisposable
{
    public abstract void Dispose();
}

public interface ICompletionRateController
{
    ValueTask<ContinuationToken> WhenAllowed(CancellationToken cancellationToken);
}