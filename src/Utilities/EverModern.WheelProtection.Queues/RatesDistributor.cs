using EverModern.Chronos;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EverModern.WheelProtection.Queues;

/// <summary>
/// Distributes calls across multiple completion rate controllers.
/// </summary>
/// <typeparam name="T">The item type to lock.</typeparam>
public class RatesDistributor<T>
{
    readonly KeyValuePair<CompletionRateController, T>[] _rateControllers;

    /// <summary>
    /// Initializes a new instance using a custom time provider.
    /// </summary>
    /// <param name="callConstraints">Pairs of items and their call constraints.</param>
    /// <param name="nowProvider">The time provider.</param>
    public RatesDistributor(IEnumerable<KeyValuePair<T, IEnumerable<CallConstraint>>> callConstraints, IChronos nowProvider)
    {
        _rateControllers = [.. callConstraints.Select(cc => KeyValuePair.Create(new CompletionRateController(cc.Value, nowProvider), cc.Key))];
    }

    /// <summary>
    /// Initializes a new instance using real time.
    /// </summary>
    /// <param name="callConstraints">Pairs of items and their call constraints.</param>
    public RatesDistributor(IEnumerable<KeyValuePair<T, IEnumerable<CallConstraint>>> callConstraints) 
        : this(callConstraints, RealTimeChronos.Instance)
    {
    }

    /// <summary>
    /// Awaits the next available item locker across all controllers.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async ValueTask<ItemLocker<T>> AwaitAnotherAsync(CancellationToken cancellationToken)
    {
        foreach (var (rateController, item) in _rateControllers)
        {
            if (rateController.TryTakeImmediately(out var locker) is true)
            {
                var result = new CallbackItemLocker<T>(item, (_) => locker.Dispose());
                return result;
            }
        }

        var cts = new CancellationTokenSource();
        cancellationToken.Register(cts.Cancel);

        var waitingForAll = _rateControllers
            .Select(rc => rc.Key.WhenAllowed(cancellationToken).AsTask().ContinueWith(t => (t.Result, rc.Value)))
            .ToArray();

        var firstCompleted = await Task.WhenAny(waitingForAll);
        cts.Cancel();

        foreach (var other in waitingForAll)
        {
            if (other != firstCompleted && (other.IsCanceled == false))
            {
                other
                    .ContinueWith(t => t.Result.Result.Dispose())
                    .GetType();
            }
        }

        return new CallbackItemLocker<T>(
            firstCompleted.Result.Value,
            (_) => firstCompleted.Result.Result.Dispose());
    }
}
