using System;
using System.Threading;
using System.Threading.Tasks;

namespace DestallMaterials.Chronos
{
    public class RealTimeChronos : IChronos
    {
        public static RealTimeChronos Instance = new RealTimeChronos();

        RealTimeChronos()
        { 
        }

        public DateTimeOffset Now => DateTimeOffset.Now;

        public Task WhenComes(DateTimeOffset targetTime, CancellationToken cancellationToken = default)
            => Task.Delay(targetTime - Now, cancellationToken);

        public Task WhenComes(DateTime targetTimeUtc, CancellationToken cancellationToken = default)
            => WhenComes(new DateTimeOffset(targetTimeUtc, default(TimeSpan)), cancellationToken);

        public Task WhenPasses(TimeSpan time, CancellationToken cancellationToken = default)
            => Task.Delay(time, cancellationToken);
    }
}
