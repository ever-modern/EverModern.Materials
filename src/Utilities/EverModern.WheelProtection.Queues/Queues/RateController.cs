using EverModern.Chronos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EverModern.Threading.Queues;

/// <summary>
/// Rate limiter that enforces call constraints within time windows.
/// </summary>
public class RateController : IRateController
{
    readonly CallConstraint[] _actionConstraints;
    readonly IChronos _nowProvider;
    readonly int _commonCallSlotsNumber;
    readonly DateTimeOffset[] _calls;
    readonly TimeSpan _longestCallDistance;
    readonly Lock _lock = new();

    /// <summary>
    /// Initializes a new instance of the rate controller with a custom time provider.
    /// </summary>
    /// <param name="actionConstraints">The call constraints to enforce.</param>
    /// <param name="nowProvider">The time provider.</param>
    public RateController(IEnumerable<CallConstraint> actionConstraints, IChronos nowProvider)
    {
        _actionConstraints = [.. actionConstraints.OptimizeConstraints()];
        _nowProvider = nowProvider;
        _commonCallSlotsNumber = _actionConstraints.Sum(a => a.MaxCallsCount);
        _calls = new DateTimeOffset[_commonCallSlotsNumber];
        _longestCallDistance = _actionConstraints.MaxBy(c => c.Period).Period;
    }

    /// <summary>
    /// Initializes a new instance of the rate controller using real time.
    /// </summary>
    /// <param name="actionConstraints">The call constraints to enforce.</param>
    public RateController(IEnumerable<CallConstraint> actionConstraints)
        : this(actionConstraints, RealTimeChronos.Instance)
    {
    }

    /// <inheritdoc />
    public async ValueTask WhenAllowed(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (TryImmediately(out var nextCallAt))
        {
            return;
        }

        await _nowProvider.WhenComes(nextCallAt, cancellationToken);
        await WhenAllowed(cancellationToken);
    }

    /// <inheritdoc />
    public bool TryImmediately(out DateTimeOffset tryAgainAt)
    {
        using var _ = new ScopeLocker(_lock);

        tryAgainAt = CalculateNextCallPossibleTime();
        var now = _nowProvider.Now;
        if (now < tryAgainAt)
        {
            return false;
        }

        CommitCall(now);
        return true;
    }

    void CommitCall(DateTimeOffset at)
    {
        var calls = _calls.AsSpan();
        calls[..^1].CopyTo(calls[1..]);
        calls[0] = at;
    }

    DateTimeOffset CalculateNextCallPossibleTime()
    {
        var l = _actionConstraints.Length;
        var actionConstraints = _actionConstraints.AsSpan();
        var calls = _calls.AsSpan();
        var now = _nowProvider.Now;
        DateTimeOffset result = default;
        for (int i = 0; i < l; i++)
        {
            var (period, callsAllowedQuantity) = actionConstraints[i];
            DateTimeOffset earliestPossibleCallByThisConstraint = default;
            var madeCallsWithinConstraint = 0;
            for (int callIndex = 0; callIndex < _commonCallSlotsNumber; callIndex++)
            {
                var call = calls[callIndex];
                var distance = now - call;
                if (distance > _longestCallDistance)
                {
                    break;
                }

                bool withinConstraint = distance < period;

                if (withinConstraint is false)
                {
                    earliestPossibleCallByThisConstraint = default;
                    break;
                }
                else
                {
                    earliestPossibleCallByThisConstraint = call + period - distance;
                    madeCallsWithinConstraint++;
                }
            }

            if (madeCallsWithinConstraint < callsAllowedQuantity)
            {
                earliestPossibleCallByThisConstraint = default;
            }

            result = result > earliestPossibleCallByThisConstraint ?
                result :
                earliestPossibleCallByThisConstraint;
        }

        return result;
    }
}
