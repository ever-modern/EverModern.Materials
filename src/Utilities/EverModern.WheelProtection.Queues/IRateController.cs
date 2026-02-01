using System;
using System.Threading;
using System.Threading.Tasks;

namespace EverModern.WheelProtection.Queues;

public interface IRateController
{
    bool TryImmediately(out DateTimeOffset tryAgainAt);
    ValueTask WhenAllowed(CancellationToken cancellationToken = default);
}
