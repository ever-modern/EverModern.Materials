using System;
using System.Threading;
using System.Threading.Tasks;

namespace DestallMaterials.WheelProtection.Queues;

public interface IRateController
{
    bool TryImmediately(out DateTimeOffset tryAgainAt);
    ValueTask WhenAllowed(CancellationToken cancellationToken = default);
}
