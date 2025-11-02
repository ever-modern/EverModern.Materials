using DestallMaterials.Chronos;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DestallMaterials.WheelProtection.Queues;

public class RatesDistributor<T>
{
    readonly KeyValuePair<CompletionRateController, T>[] _rateControllers;

    public RatesDistributor(IEnumerable<KeyValuePair<T, IEnumerable<CallConstraint>>> callConstraints, IChronos nowProvider)
    {
        _rateControllers = [.. callConstraints.Select(cc => KeyValuePair.Create(new CompletionRateController(cc.Value, nowProvider), cc.Key))];
    }

    public RatesDistributor(IEnumerable<KeyValuePair<T, IEnumerable<CallConstraint>>> callConstraints) 
        : this(callConstraints, RealTimeChronos.Instance)
    {
    }

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
