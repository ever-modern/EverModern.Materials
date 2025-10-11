using DestallMaterials.WheelProtection.Queues;

public class ServerEmulator
{
    readonly IEnumerable<CallConstraint> _constraints;
    readonly List<StartAndFinishTime> _processed = [];

    object _lock = new object();

    public ServerEmulator(IEnumerable<CallConstraint> constraints)
    {
        _constraints = Algorithms.OptimizeConstraints(constraints);
    }

    public async Task<long> ProcessRequestAsync(TimeSpan executionLength)
    {
        await Task.Delay(150);
        var date = DateTime.UtcNow;
        lock (_lock)
        {
            if (_constraints.Any(constraint => _processed.Count(p =>
            {
                bool notFinished = p.Finish == default;
                bool finishedJustYet = date - p.Finish < constraint.Period;
                bool startedJustYet = (date - p.Start < constraint.Period);
                return (notFinished || finishedJustYet || startedJustYet);
            }) >= constraint.MaxCallsCount))
            {
                throw new Exception();
            }
        }
        var times = new StartAndFinishTime(date);
        _processed.Add(times);
        await Task.Delay(executionLength);
        times.Finish = DateTime.UtcNow;
        return executionLength.Ticks;
    }
}