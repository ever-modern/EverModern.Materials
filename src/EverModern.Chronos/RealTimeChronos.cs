namespace EverModern.Chronos
{
    /// <summary>
    /// Chronos implementation that uses real system time.
    /// </summary>
    public class RealTimeChronos : IChronos
    {
        /// <summary>
        /// Gets the shared instance.
        /// </summary>
        public static RealTimeChronos Instance = new RealTimeChronos();

        RealTimeChronos()
        { 
        }

        /// <inheritdoc />
        public DateTimeOffset Now => DateTimeOffset.Now;

        /// <inheritdoc />
        public Task WhenComes(DateTimeOffset targetTime, CancellationToken cancellationToken = default)
            => Task.Delay(targetTime - Now, cancellationToken);

        /// <inheritdoc />
        public Task WhenComes(DateTime targetTimeUtc, CancellationToken cancellationToken = default)
            => WhenComes(new DateTimeOffset(targetTimeUtc, default(TimeSpan)), cancellationToken);

        /// <inheritdoc />
        public Task WhenPasses(TimeSpan time, CancellationToken cancellationToken = default)
            => Task.Delay(time, cancellationToken);
    }
}
