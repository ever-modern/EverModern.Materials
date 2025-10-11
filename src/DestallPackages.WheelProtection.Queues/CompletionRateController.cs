using DestallMaterials.Chronos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DestallMaterials.WheelProtection.Queues;

public class CompletionRateController : ICompletionRateController
{
    void Log(string message)
    {
#if DEBUG
        Console.WriteLine($"{nameof(CompletionRateController)}#{GetHashCode()} {_nowProvider.Now}: {message}");
#endif
    }

    readonly IChronos _nowProvider;
    readonly CallConstraint[] _actionConstraints;
    readonly List<StartAndFinishTime> _history = [];
    readonly int _commonCallSlotsNumber;

    async Task ProcessAnotherAsync()
    {
        var nextCallAt = CalculateNextCallAllowedTime();
        if (nextCallAt < DateTimeOffset.MaxValue && _subscriptions.TryDequeue(out var sub))
        {
            Log($"Task {sub.Item2.GetHashCode()} will have to wait until {nextCallAt} to complete.");
            await _nowProvider.WhenComes(nextCallAt).ContinueWith((_) => sub.Item1());
        }
    }

    readonly ControlledQueue<Tuple<Action, Task>> _subscriptions
        = new(x => x.Item2.IsCompleted == false);

    readonly Lock _locker = new();

    public CompletionRateController(IEnumerable<CallConstraint> actionConstraints, IChronos nowProvider)
    {
        _actionConstraints = [.. actionConstraints.OptimizeConstraints()];
        _nowProvider = nowProvider;
        _commonCallSlotsNumber = _actionConstraints.Sum(a => a.MaxCallsCount);
    }

    public CompletionRateController(IEnumerable<CallConstraint> actionConstraints)
        : this(actionConstraints, RealTimeChronos.Instance)
    {
    }

    public ValueTask<ContinuationToken> WhenAllowed(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (TryTakeImmediately(out var immediateResult))
        {
            Log("Immediate result returned.");
            return new(immediateResult);
        }

        lock (_locker)
        {
            var tcs = new TaskCompletionSource<ContinuationToken>();

            cancellationToken.Register(() => tcs.TrySetCanceled());

            void action()
            {
                if (tcs.Task.IsCompleted)
                {
                    return;
                }

                var result = CreateControlLocker();
                tcs.TrySetResult(result);
            }

            bool willBeNext = _subscriptions.FindThrough() is null;
            if (willBeNext)
            {
                Log($"Task {tcs.Task.GetHashCode()} is going to be next in line.");
            }

            var subscriptionTask = Tuple.Create(action, tcs.Task as Task);
            _subscriptions.Enqueue(subscriptionTask);

            try
            {
                return new(tcs.Task);
            }
            finally
            {
                if (willBeNext)
                {
                    ProcessAnotherAsync();
                }
            }
        }
    }

    public bool TryTakeImmediately(out ContinuationToken result)
    {
        lock (_locker)
        {
            var nextPossibleCallAt = CalculateNextCallAllowedTime();
            if (nextPossibleCallAt == default)
            {
                result = CreateControlLocker();
                return true;
            }

            result = null;
            return false;
        }
    }

    RateControlLocker CreateControlLocker()
    {
        var time = new StartAndFinishTime(_nowProvider.Now);
        _history.Add(time);
        return new RateControlLocker(() =>
        {
            lock (_locker)
            {
                time.Finish = _nowProvider.Now;
                ProcessAnotherAsync();
            }
        });
    }

    /// <summary>
    /// Calculate time in which the next call can be made.
    /// </summary>
    /// <returns>TimeSpan.MinValue - if call may be made immediately</returns>
    DateTimeOffset CalculateNextCallAllowedTime()
    {
        var executionsHistory = _history;
        var callsCount = executionsHistory.Count;
        Span<CallConstraint> actionConstraints = _actionConstraints;
        var now = _nowProvider.Now;
        if (executionsHistory.Count == 0)
        {
            return default;
        }

        DateTimeOffset earliestCallPossible = default;
        for (int i = 0; i < actionConstraints.Length; i++)
        {
            var (period, callsAllowedQuantity) = actionConstraints[i];
            var allowedNextCallByThisConstraint = DateTimeOffset.MaxValue;
            var callsWithinConstraint = 0;
            for (int callIndex = callsCount - 1; callsCount - callIndex <= _commonCallSlotsNumber; callIndex--)
            {
                var (_, finish) = executionsHistory[callIndex];
                var isOver = now >= finish;
                var withinConstraint = isOver || now - finish <= period;
                if (withinConstraint is false)
                {
                    allowedNextCallByThisConstraint = default;
                    break;
                }
                else 
                {
                    callsWithinConstraint++;
                }

                if (isOver)
                {
                    allowedNextCallByThisConstraint = finish + period;
                    break;
                }
            }

            if (callsWithinConstraint < callsAllowedQuantity)
            {
                allowedNextCallByThisConstraint = default;
            }

            if (allowedNextCallByThisConstraint == DateTimeOffset.MaxValue)
            {
                return DateTimeOffset.MaxValue;
            }

            earliestCallPossible = earliestCallPossible > allowedNextCallByThisConstraint ?
                earliestCallPossible :
                allowedNextCallByThisConstraint;
        }

        return earliestCallPossible;
    }
}