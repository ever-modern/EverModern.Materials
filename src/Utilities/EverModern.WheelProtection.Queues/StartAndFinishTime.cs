using System;
using DateTime = System.DateTimeOffset;

namespace EverModern.Threading;

/// <summary>
/// Represents a time interval with start and finish timestamps.
/// </summary>
public class StartAndFinishTime
{
    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public DateTime Start { get; set; }
    /// <summary>
    /// Gets or sets the finish time.
    /// </summary>
    public DateTime Finish { get; set; }

    /// <summary>
    /// Initializes a new instance with start and finish times.
    /// </summary>
    /// <param name="start">The start time.</param>
    /// <param name="finish">The finish time.</param>
    public StartAndFinishTime(DateTime start, DateTime finish)
    {
        Start = start;
        Finish = finish;
    }

    /// <summary>
    /// Initializes a new instance with a start time and an open-ended finish.
    /// </summary>
    /// <param name="start">The start time.</param>
    public StartAndFinishTime(DateTime start) : this(start, DateTime.MaxValue)
    {
    }

    /// <summary>
    /// Gets the duration when both start and finish are set.
    /// </summary>
    public TimeSpan Duration => (Start > default(DateTime) && Finish > default(DateTime)) ? Finish - Start : default;

    /// <summary>
    /// Deconstructs the interval into start and finish times.
    /// </summary>
    /// <param name="start">The start time.</param>
    /// <param name="finish">The finish time.</param>
    public void Deconstruct(out DateTime start, out DateTime finish)
    {
        start = Start;
        finish = Finish;
    }
}