using System;
using System.Threading;
using System.Threading.Tasks;

namespace DestallMaterials.Chronos
{
    public interface IChronos
    {
        DateTimeOffset Now { get; }

        Task WhenPasses(TimeSpan time, CancellationToken cancellationToken = default);

        Task WhenComes(DateTimeOffset targetTime, CancellationToken cancellationToken = default);

        Task WhenComes(DateTime targetTimeUtc, CancellationToken cancellationToken = default);
    }

    public interface IChronosControll
    {
        void SetTime(DateTimeOffset newNow);

        void MoveTime(TimeSpan moveForward);
    }
}
