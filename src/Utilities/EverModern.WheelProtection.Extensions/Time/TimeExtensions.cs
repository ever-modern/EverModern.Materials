using System.Runtime.CompilerServices;

namespace EverModern.WheelProtection.Extensions.Time;

/// <summary>
/// Provides convenience extensions for date and time types.
/// </summary>
public static class TimeExtensions
{
    /// <summary>
    /// Awaits a delay equal to the timespan.
    /// </summary>
    /// <param name="time">The delay duration.</param>
    public static TaskAwaiter GetAwaiter(this TimeSpan time)
       => Task.Delay(time).GetAwaiter();

    /// <summary>
    /// Awaits until the specified time.
    /// </summary>
    /// <param name="dateTime">The target time.</param>
    public static TaskAwaiter GetAwaiter(this DateTime dateTime)
        => Task.Delay(dateTime - DateTime.UtcNow).GetAwaiter();

    /// <summary>
    /// Rounds up to the nearest measure.
    /// </summary>
    /// <param name="dateTime">The time to round.</param>
    /// <param name="measure">The rounding interval.</param>
    public static DateTime RoundUpTo(this DateTime dateTime, TimeSpan measure)
    {
        var ticks = (dateTime - default(DateTime)).Ticks;
        var intResult = ticks / measure.Ticks;
        var result = intResult * measure.Ticks == ticks ? intResult * measure.Ticks : intResult + 1;
        return default(DateTime) + TimeSpan.FromTicks(result);
    }

    /// <summary>
    /// Rounds down to the nearest measure.
    /// </summary>
    /// <param name="dateTime">The time to round.</param>
    /// <param name="measure">The rounding interval.</param>
    public static DateTime RoundDownTo(this DateTime dateTime, TimeSpan measure)
    {
        var ticks = (dateTime - default(DateTime)).Ticks;
        var intResult = ticks / measure.Ticks;
        var result = intResult * measure.Ticks == ticks ? intResult * measure.Ticks : intResult - 1;
        return default(DateTime) + TimeSpan.FromTicks(result);
    }

    /// <summary>
    /// Rounds to the nearest measure.
    /// </summary>
    /// <param name="dateTime">The time to round.</param>
    /// <param name="measure">The rounding interval.</param>
    public static DateTime RoundTo(this DateTime dateTime, TimeSpan measure)
    {
        var ticks = (dateTime - default(DateTime)).Ticks;
        var intResult = (int)Math.Round((decimal)ticks / measure.Ticks);
        var result = intResult * measure.Ticks == ticks ? intResult * measure.Ticks : intResult - 1;
        return default(DateTime) + TimeSpan.FromTicks(result);
    }
}
