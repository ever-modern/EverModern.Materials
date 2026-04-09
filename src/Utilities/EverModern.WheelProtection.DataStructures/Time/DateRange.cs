using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DateTime = System.DateTime;

namespace EverModern.WheelProtection.DataStructures.Time;

/// <summary>
/// Represents a date-only range.
/// </summary>
public readonly struct DateRange : IEquatable<DateRange>
{
    /// <summary>
    /// Gets the start date.
    /// </summary>
    public DateOnly Start { get; }
    /// <summary>
    /// Gets the end date.
    /// </summary>
    public DateOnly End { get; }

    [Obsolete("Do not call parameterless constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public DateRange() { }

    /// <summary>
    /// Initializes a new range.
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    public DateRange(DateOnly start, DateOnly end)
    {
        if (Start > End)
        {
            throw new ArgumentException("Start value can't be greater that End value.");
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets the duration in days.
    /// </summary>
    public int DurationDays => End.DayNumber - Start.DayNumber;

    /// <summary>
    /// Splits the range into segments of the specified length.
    /// </summary>
    /// <param name="days">The segment length in days.</param>
    public DateRange[] Split(int days)
    {
        var n = (int)Math.Ceiling((decimal)DurationDays / days);
        var result = new DateRange[n];
        var current = new DateRange(Start, Start.AddDays(days));
        result[0] = current;
        for (int i = 1; i < n; i++)
        {
            current = new(current.Start.AddDays(days), current.End.AddDays(days));
            result[i] = current;
        }
        var last = result[n - 1];
        result[n - 1] = new(last.Start, End);

        return result;
    }

    /// <summary>
    /// Determines whether the range contains the specified date.
    /// </summary>
    /// <param name="date">The date to test.</param>
    public bool Contains(DateOnly date) => date <= End && date >= Start;

    /// <summary>
    /// Clamps the specified date to the range.
    /// </summary>
    /// <param name="date">The date to clamp.</param>
    public DateOnly Clamp(DateOnly date)
    {
        if (date < Start)
        {
            return Start;
        }

        if (date > End)
        {
            return End;
        }

        return date;
    }

    /// <summary>
    /// Merges contiguous date ranges into a single range.
    /// </summary>
    /// <param name="ranges">The ranges to merge.</param>
    public static DateRange Merge(params IEnumerable<DateRange> ranges)
    {
        ranges = ranges.OrderBy(x => x.Start);
        var resultStart = DateOnly.MinValue;
        var resultEnd = DateOnly.MaxValue;
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

        return new DateRange(resultStart, resultEnd);
    }

    /// <summary>
    /// Converts a tuple to a date range.
    /// </summary>
    /// <param name="other">The tuple.</param>
    public static implicit operator DateRange((DateOnly start, DateOnly end) other) =>
        new(other.start, other.end);

    /// <summary>
    /// Converts the date range to a date-time range using the provided times.
    /// </summary>
    /// <param name="startTime">The time to use for the start.</param>
    /// <param name="endTime">The time to use for the end.</param>
    public DateTimeRange ToDateTimeRange(TimeOnly startTime = default, TimeOnly endTime = default)
        => new(Start.ToDateTime(startTime), End.ToDateTime(endTime));

    /// <inheritdoc />
    public static bool operator ==(DateRange left, DateRange right) => left.Equals(right);

    /// <summary>
    /// Combines overlapping ranges.
    /// </summary>
    /// <param name="left">The left range.</param>
    /// <param name="other">The right range.</param>
    public static DateRange operator +(DateRange left, DateRange other)
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

    /// <summary>
    /// Splits the range by removing the overlap with another range.
    /// </summary>
    /// <param name="other">The range to split by.</param>
    public (DateRange Left, DateRange Right) SplitBy(DateRange other)
    {
        if (Intersects(other) is false)
        {
            return default;
        }

        var left = default(DateRange);
        var right = default(DateRange);

        if (other.Start > Start)
        {
            left = new DateRange(Start, other.Start);
        }

        if (other.End < End)
        {
            right = new DateRange(other.End, End);
        }

        return (left, right);
    }

    /// <inheritdoc />
    public static bool operator !=(DateRange left, DateRange right) => !(left == right);

    /// <summary>
    /// Deconstructs the range into start and end dates.
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    public void Deconstruct(out DateOnly start, out DateOnly end)
    {
        start = this.Start;
        end = this.End;
    }

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is DateRange dtr && Start == dtr.Start && End == dtr.End;

    /// <inheritdoc />
    public bool Equals(DateRange other) => Start.Equals(other.Start) && End.Equals(other.End);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Start, End);

    /// <inheritdoc />
    public override string ToString() => $"{Start} - {End}";

    /// <summary>
    /// Determines whether ranges intersect.
    /// </summary>
    /// <param name="other">The other range.</param>
    public bool Intersects(DateRange other) =>
        End >= other.Start && Start <= other.Start || other.End >= Start && other.Start <= Start;

    /// <summary>
    /// Determines whether ranges intersect and returns the intersection.
    /// </summary>
    /// <param name="other">The other range.</param>
    /// <param name="intersection">The intersection range.</param>
    public bool Intersects(DateRange other, out DateRange intersection)
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
    /// Removes the overlap of another range.
    /// </summary>
    /// <param name="other">The range to subtract.</param>
    public DateRange Subtract(DateRange other)
    {
        if (!Intersects(other))
        {
            return this;
        }

        if (other.Start > Start && other.End < End)
        {
            return new DateRange(Start, other.Start);
        }

        if (other.Start <= Start)
        {
            return new DateRange(other.End, End);
        }

        return new DateRange(Start, other.Start);
    }

    /// <summary>
    /// Shifts the range by a number of days.
    /// </summary>
    /// <param name="offsetDays">The number of days to shift.</param>
    public DateRange Shift(int offsetDays) => new(Start.AddDays(offsetDays), End.AddDays(offsetDays));

    /// <summary>
    /// Expands the range by a number of days on each side.
    /// </summary>
    /// <param name="offsetDays">The number of days to expand.</param>
    public DateRange Expand(int offsetDays) => new(Start.AddDays(-offsetDays), End.AddDays(offsetDays));

    /// <summary>
    /// Creates ranges from a sequence of dates.
    /// </summary>
    /// <param name="dates">The input dates.</param>
    public static IEnumerable<DateRange> CreateRanges(params IEnumerable<DateOnly> dates)
    {
        DateOnly? previous = default;
        foreach (var current in dates)
        {
            if (previous is not null)
            {
                yield return new DateRange(previous.Value, current);
            }

            previous = current;
        }
    }

    /// <summary>
    /// Enumerates date points that fall on the given period boundaries within the range.
    /// </summary>
    /// <param name="periodDays">The size of each period in days.</param>
    /// <param name="offsetDays">The offset applied to each period boundary.</param>
    /// <remarks>
    /// The first returned value is the first boundary at or after <see cref="Start"/>. Values are
    /// returned up to and including <see cref="End"/> when a boundary lands exactly on it.
    /// </remarks>
    public IEnumerable<DateOnly> TakeCalendarPeriodPoints(int periodDays, int offsetDays = 0)
    {
        if (offsetDays >= periodDays)
        {
            throw new ArgumentOutOfRangeException(nameof(offsetDays), "The period offset must be less than the period size.");
        }

        var next = Start.DayNumber + (periodDays - Start.DayNumber % periodDays) + offsetDays;

        while (next <= End.DayNumber)
        {
            yield return DateOnly.FromDayNumber(next);
            next += periodDays;
        }
    }
}
