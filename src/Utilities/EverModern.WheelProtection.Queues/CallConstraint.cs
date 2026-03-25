using System;
using System.Collections.Generic;

namespace EverModern.WheelProtection.Queues
{
    /// <summary>
    /// Defines a rate limiting constraint for a period and maximum call count.
    /// </summary>
    /// <param name="Period">The time window for the constraint.</param>
    /// <param name="MaxCallsCount">The maximum number of calls allowed in the period.</param>
    public record struct CallConstraint(TimeSpan Period, int MaxCallsCount)
    {
        /// <summary>
        /// Converts a key/value pair into a call constraint.
        /// </summary>
        /// <param name="other">The source pair of period and max call count.</param>
        public static implicit operator CallConstraint(KeyValuePair<TimeSpan, int> other)
            => new(other.Key, other.Value);
    }
}
