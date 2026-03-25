namespace EverModern.Chronos
{
    /// <summary>
    /// Provides time-related operations for scheduling.
    /// </summary>
    public interface IChronos
    {
        /// <summary>
        /// Gets the current time.
        /// </summary>
        DateTimeOffset Now { get; }

        /// <summary>
        /// Awaits a duration from the current time.
        /// </summary>
        /// <param name="time">The duration to wait.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task WhenPasses(TimeSpan time, CancellationToken cancellationToken = default);

        /// <summary>
        /// Awaits until the specified time.
        /// </summary>
        /// <param name="targetTime">The target time.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task WhenComes(DateTimeOffset targetTime, CancellationToken cancellationToken = default);

        /// <summary>
        /// Awaits until the specified UTC time.
        /// </summary>
        /// <param name="targetTimeUtc">The target UTC time.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task WhenComes(DateTime targetTimeUtc, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Provides control over a mutable chronos instance.
    /// </summary>
    public interface IChronosControll
    {
        /// <summary>
        /// Sets the current time.
        /// </summary>
        /// <param name="newNow">The new current time.</param>
        void SetTime(DateTimeOffset newNow);

        /// <summary>
        /// Moves the current time forward.
        /// </summary>
        /// <param name="moveForward">The duration to move forward.</param>
        void MoveTime(TimeSpan moveForward);
    }
}
