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
        var n = (int)Math.Ceiling((decimal)DurationDays / DurationDays);
        var result = new DateRange[n];
        var current = new DateRange(Start, Start.AddDays(DurationDays));
        result[0] = current;
        for (int i = 1; i < n; i++)
        {
            current = new(current.Start.AddDays(DurationDays), current.End.AddDays(DurationDays));
            result[i] = current;
        }
        var last = result[n - 1];
        result[n - 1] = new(last.Start, End);

        return result;
    }

    //public bool Contains(DateTime dateTime) => dateTime.Date.Da <= End && dateTime >= Start;

    /// <summary>
    /// Merges contiguous date-time ranges into a single range.
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
    /// Converts a tuple to a date range.
    /// </summary>
    /// <param name="other">The tuple.</param>
    public static implicit operator DateRange((DateOnly start, DateOnly end) other) =>
        new(other.start, other.end);

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
}
