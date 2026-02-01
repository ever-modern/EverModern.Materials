using System;
using System.Collections.Generic;

namespace EverModern.WheelProtection.Queues
{
    public record struct CallConstraint(TimeSpan Period, int MaxCallsCount)
    {
        public static implicit operator CallConstraint(KeyValuePair<TimeSpan, int> other)
            => new(other.Key, other.Value);
    }

    
}
