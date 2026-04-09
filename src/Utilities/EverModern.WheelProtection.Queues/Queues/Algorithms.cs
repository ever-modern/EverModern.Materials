using System;
using System.Collections.Generic;
using System.Linq;

namespace EverModern.Threading.Queues
{
    /// <summary>
    /// Provides queue-related helper algorithms.
    /// </summary>
    public static class Algorithms
    {
        /// <summary>
        /// Removes redundant constraints that are implied by shorter windows.
        /// </summary>
        /// <param name="constraints">The constraints to optimize.</param>
        public static IReadOnlyList<CallConstraint> OptimizeConstraints(this IEnumerable<CallConstraint> constraints)
        {
            constraints = [.. constraints.OrderByDescending(c => c.Period)];

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
