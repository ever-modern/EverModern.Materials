using System.Diagnostics.CodeAnalysis;
using DateTime = System.DateTime;

namespace EverModern.WheelProtection.DataStructures.Time;

public readonly struct DateTimeRange : IEquatable<DateTimeRange>
{
    public DateTime Start { get; }
    public DateTime End { get; }

    [Obsolete("Do not call parameterless constructor.", true)]
    public DateTimeRange() { }

    public DateTimeRange(DateTime start, DateTime end)
    {
        if (Start > End)
        {
            throw new ArgumentException("Start value can't be greater that End value.");
        }

        Start = start;
        End = end;
    }

    public TimeSpan Duration => End - Start;

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

    public bool Contains(DateTime dateTime) => dateTime <= End && dateTime >= Start;

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

    public static implicit operator DateTimeRange((DateTime start, DateTime end) other) =>
        new(other.start, other.end);

    public static bool operator ==(DateTimeRange left, DateTimeRange right) => left.Equals(right);

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

    public static bool operator !=(DateTimeRange left, DateTimeRange right) => !(left == right);

    public void Deconstruct(out DateTime start, out DateTime end)
    {
        start = this.Start;
        end = this.End;
    }

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is DateTimeRange dtr && Start == dtr.Start && End == dtr.End;

    public bool Equals(DateTimeRange other) => Start.Equals(other.Start) && End.Equals(other.End);

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public override string ToString() => $"{Start} - {End}";

    public bool Intersects(DateTimeRange other) =>
        End >= other.Start && Start <= other.Start || other.End >= Start && other.Start <= Start;

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
}
