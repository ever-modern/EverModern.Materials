using DestallMaterials.WheelProtection.Queues;

namespace DestallMaterials.Tests;
public static class CallConstraintExtensions
{
    public static IEnumerable<(CallConstraint, DateTime[])> FindViolatedConstraints(
        this IEnumerable<CallConstraint> callConstraints, IEnumerable<DateTime> calls)
    {
        calls = calls.OrderDescending();
        foreach (var (period, quantityAllowed) in callConstraints)
        {
            var madeCalls = 0;
            foreach (var call in calls)
            {
                var callsInVicinity = calls.Where(c => c > call - period && c <= call).ToArray();
                if (callsInVicinity.Length > quantityAllowed)
                {
                    yield return (new(period, quantityAllowed), callsInVicinity);
                    continue;
                }
            }
        }
    }
}