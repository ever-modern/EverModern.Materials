using System;
using System.Collections.Generic;
using System.Linq;

namespace DestallMaterials.WheelProtection.Queues
{
    public static class Algorithms
    {
        public static IReadOnlyList<CallConstraint> OptimizeConstraints(this IEnumerable<CallConstraint> constraints)
        {
            constraints = constraints.OrderByDescending(c => c.Period).ToArray();

            var result = constraints.Where(constraint =>
            {
                var shorterConstraints = constraints.Where(c => c.Period < constraint.Period);

                var limitIsContainedWithin = shorterConstraints.FirstOrDefault(
                    sc => Math.Ceiling(constraint.Period.Ticks / (double)sc.Period.Ticks) * sc.MaxCallsCount < constraint.MaxCallsCount);

                return limitIsContainedWithin.MaxCallsCount == default;
            }).ToList();

            return result;
        }
    }
}
