using System;
using System.Collections.Generic;
using System.Linq;

namespace DestallMaterials.WheelProtection.Queues
{
    public record struct CallConstraint(TimeSpan Period, int MaxCallsCount)
    {
        public static implicit operator CallConstraint(KeyValuePair<TimeSpan, int> other)
            => new(other.Key, other.Value);
    }

    
}
