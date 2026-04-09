using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace EverModern.WheelProtection.DataStructures.Time;

/// <summary>
/// Represents a date-time range.
/// </summary>
public readonly struct DateTimeRange : IEquatable<DateTimeRange>
{
    /// <summary>
    /// Gets the start time.
    /// </summary>
    public DateTime Start { get; }
    /// <summary>
    /// Gets the end time.
    /// </summary>
    public DateTime End { get; }

    [Obsolete("Do not call parameterless constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public DateTimeRange() { }

    /// <summary>
    /// Initializes a new range.
    /// </summary>
    /// <param name="start">The start time.</param>
    /// <param name="end">The end time.</param>
    public DateTimeRange(DateTime start, DateTime end)
    {
        if (Start > End)
        {
            throw new ArgumentException("Start value can't be greater that End value.");
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets the duration.
    /// </summary>
    public TimeSpan Duration => End - Start;

    /// <summary>
    /// Splits the range into segments of the specified duration.
    /// </summary>
    /// <param name="duration">The segment duration.</param>
    public DateTimeRange[] Split(TimeSpan duration)
    {
        var n = (int)Math.Ceiling((decimal)Duration.Ticks / duration.Ticks);
        var result = new DateTimeRange[n];
        var current = new DateTimeRange(Start, Start + duration);
        result[0] = current;
        for (int i = 1; i < n; i++)
        {
            current = new(current.Start + duration, current.End + duration);
            result[i] = current;
        }
        var last = result[n - 1];
        result[n - 1] = new(last.Start, End);

        return result;
    }

    /// <summary>
    /// Determines whether the range contains the specified time.
    /// </summary>
    /// <param name="dateTime">The time to test.</param>
    public bool Contains(DateTime dateTime) => dateTime <= End && dateTime >= Start;

    /// <summary>
    /// Clamps the specified time to the range.
    /// </summary>
    /// <param name="dateTime">The time to clamp.</param>
    public DateTime Clamp(DateTime dateTime)
    {
        if (dateTime < Start)
        {
            return Start;
        }

        if (dateTime > End)
        {
            return End;
        }

        return dateTime;
    }

    /// <summary>
    /// Merges contiguous ranges into a single range.
    /// </summary>
    /// <param name="ranges">The ranges to merge.</param>
    public static DateTimeRange Merge(params IEnumerable<DateTimeRange> ranges)
    {
        ranges = ranges.OrderBy(x => x.Start);
        var resultStart = DateTime.MinValue;
        var resultEnd = DateTime.MaxValue;
        int i = 0;
        foreach (var (start, end) in ranges)
        {
            if (start > resultEnd)
            {
                throw new ArgumentException($"Date {resultEnd} preceeds {start}.");
            }

            if (i++ == 0)
            {
                resultStart = start;
            }

            resultEnd = end;
        }

        return new DateTimeRange(resultStart, resultEnd);
    }

    /// <summary>
    /// Converts a tuple to a date-time range.
    /// </summary>
    /// <param name="other">The tuple.</param>
    public static implicit operator DateTimeRange((DateTime start, DateTime end) other) =>
        new(other.start, other.end);

    /// <summary>
    /// Converts the date-time range to a date range.
    /// </summary>
    public DateRange ToDateRange()
        => new(DateOnly.FromDateTime(Start), DateOnly.FromDateTime(End));

    /// <inheritdoc />
    public static bool operator ==(DateTimeRange left, DateTimeRange right) => left.Equals(right);

    /// <summary>
    /// Combines overlapping ranges.
    /// </summary>
    /// <param name="left">The left range.</param>
    /// <param name="other">The right range.</param>
    public static DateTimeRange operator +(DateTimeRange left, DateTimeRange other)
    {
        if (left.End >= other.Start && left.Start <= other.Start)
        {
            return new(other.Start, left.Start);
        }
        else if (other.End >= left.Start && other.Start <= left.Start)
        {
            return new(left.Start, other.Start);
        }
        else
        {
            return default;
        }
    }

    /// <inheritdoc />
    public static bool operator !=(DateTimeRange left, DateTimeRange right) => !(left == right);

    /// <summary>
    /// Deconstructs the range into start and end times.
    /// </summary>
    /// <param name="start">The start time.</param>
    /// <param name="end">The end time.</param>
    public void Deconstruct(out DateTime start, out DateTime end)
    {
        start = this.Start;
        end = this.End;
    }

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is DateTimeRange dtr && Start == dtr.Start && End == dtr.End;

    /// <inheritdoc />
    public bool Equals(DateTimeRange other) => Start.Equals(other.Start) && End.Equals(other.End);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Start, End);

    /// <inheritdoc />
    public override string ToString() => $"{Start} - {End}";

    /// <summary>
    /// Determines whether ranges intersect.
    /// </summary>
    /// <param name="other">The other range.</param>
    public bool Intersects(DateTimeRange other) =>
        End >= other.Start && Start <= other.Start || other.End >= Start && other.Start <= Start;

    /// <summary>
    /// Determines whether ranges intersect and returns the intersection.
    /// </summary>
    /// <param name="other">The other range.</param>
    /// <param name="intersection">The intersection range.</param>
    public bool Intersects(DateTimeRange other, out DateTimeRange intersection)
    {
        if (End >= other.Start && Start <= other.Start)
        {
            intersection = new(other.Start, End);
            return true;
        }
        else if (other.End >= Start && other.Start <= Start)
        {
            intersection = new(Start, other.End);
            return true;
        }
        else
        {
            intersection = default;
            return false;
        }
    }

    /// <summary>
    /// Splits the range by removing the overlap with another range.
    /// </summary>
    /// <param name="other">The range to split by.</param>
    public (DateTimeRange Left, DateTimeRange Right) SplitBy(DateTimeRange other)
    {
        if (Intersects(other) is false)
        {
            return Start < other.Start ? (this, default) : (default, this);
        }

        var left = default(DateTimeRange);
        var right = default(DateTimeRange);

        if (other.Start > Start)
        {
            left = new DateTimeRange(Start, other.Start);
        }

        if (other.End < End)
        {
            right = new DateTimeRange(other.End, End);
        }

        return (left, right);
    }

    public DateTimeRange Shift(TimeSpan offset) => new(Start + offset, End + offset);

    public DateTimeRange Expand(TimeSpan offset) => new(Start - offset, End + offset);

    public static IEnumerable<DateTimeRange> CreateRanges(params IEnumerable<DateTime> dateTimes)
    {
        DateTime? previous = default;
        foreach (var current in dateTimes)
        {
            if (previous is not null)
            {
                yield return new DateTimeRange(previous.Value, current);
            }

            previous = current;
        }
    }

    /// <summary>
    /// Enumerates time points that fall on the given calendar period boundaries within the range.
    /// </summary>
    /// <param name="periodSize">The size of each calendar period.</param>
    /// <param name="periodOffset">The offset applied to each period boundary.</param>
    /// <remarks>
    /// The first returned value is the first boundary at or after <see cref="Start"/>. Values are
    /// returned up to and including <see cref="End"/> when a boundary lands exactly on it.
    /// </remarks>
    public IEnumerable<DateTime> TakeCalendarPeriodPoints(TimeSpan periodSize, TimeSpan periodOffset = default)
    {
        if (periodOffset >= periodSize)
        {
            throw new ArgumentOutOfRangeException(nameof(periodOffset), "The period offset must be less than the period size.");
        }

        var periodTicks = periodSize.Ticks;

        var next = Start.Ticks + (periodTicks - Start.Ticks % periodTicks) + periodOffset.Ticks;

        while (next <= End.Ticks)
        {
            yield return new DateTime(next);
            next += periodTicks;
        }
    }
}