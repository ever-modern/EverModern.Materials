namespace DestallMaterials.WheelProtection.Extensions.Ranges;

public static class RangeExtensions
{
    public static IEnumerable<int> AsSequence(this System.Range range)
    {
        var (start, end) = range;
        if (start.IsFromEnd || end.IsFromEnd)
        {
            throw new ArgumentException("Can't apply from-end indexing without related length specified.");
        }
        for (int i = start.Value; i < end.Value; i++)
        {
            yield return i;
        }
    }

    public static IEnumerable<int> AsSequence(this System.Range range, int relatedLength)
    {
        var (start, end) = range.GetOffsetAndLength(relatedLength);
        for (int i = start; 
            i < end; 
            i++)
        {
            yield return i;
        }
    }

    public static void Deconstruct(this System.Range range, out Index start, out Index end)
    {
        start = range.Start;
        end = range.End;
    }

    public static IEnumerator<int> GetEnumerator(this System.Range range)
    {
        var (start, end) = range;
        if (start.IsFromEnd || end.IsFromEnd)
        {
            throw new ArgumentException("Can't apply from-end indexing without related length specified.");
        }
        for (int i = start.Value; i < end.Value; i++)
        {
            yield return i;
        }
    }

    public static IEnumerable<T> Select<T>(this Range range, Func<int, T> selector)
        => range.AsSequence().Select(selector); 
}
