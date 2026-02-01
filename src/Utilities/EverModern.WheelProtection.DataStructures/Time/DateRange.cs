using System.Diagnostics.CodeAnalysis;
using DateTime = System.DateTime;

namespace EverModern.WheelProtection.DataStructures.Time;

public readonly struct DateRange : IEquatable<DateRange>
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    [Obsolete("Do not call parameterless constructor.", true)]
    public DateRange() { }

    public DateRange(DateOnly start, DateOnly end)
    {
        if (Start > End)
        {
            throw new ArgumentException("Start value can't be greater that End value.");
        }

        Start = start;
        End = end;
    }

    public int DurationDays => End.DayNumber - Start.DayNumber;

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

    public static implicit operator DateRange((DateOnly start, DateOnly end) other) =>
        new(other.start, other.end);

    public static bool operator ==(DateRange left, DateRange right) => left.Equals(right);

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

    public static bool operator !=(DateRange left, DateRange right) => !(left == right);

    public void Deconstruct(out DateOnly start, out DateOnly end)
    {
        start = this.Start;
        end = this.End;
    }

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is DateRange dtr && Start == dtr.Start && End == dtr.End;

    public bool Equals(DateRange other) => Start.Equals(other.Start) && End.Equals(other.End);

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public override string ToString() => $"{Start} - {End}";

    public bool Intersects(DateRange other) =>
        End >= other.Start && Start <= other.Start || other.End >= Start && other.Start <= Start;

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
