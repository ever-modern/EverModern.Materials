using EverModern.WheelProtection.Extensions.Ranges;

namespace EverModern.WheelProtection.Extensions.Ranges;

/// <summary>
/// Provides extension methods for <see cref="System.Range"/>.
/// </summary>
public static class RangeExtensions
{
    /// <summary>
    /// Enumerates indexes within the range.
    /// </summary>
    /// <param name="range">The range.</param>
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

    /// <summary>
    /// Enumerates indexes within the range using a related length.
    /// </summary>
    /// <param name="range">The range.</param>
    /// <param name="relatedLength">The related length.</param>
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

    /// <summary>
    /// Deconstructs a range into start and end indexes.
    /// </summary>
    /// <param name="range">The range.</param>
    /// <param name="start">The start index.</param>
    /// <param name="end">The end index.</param>
    public static void Deconstruct(this System.Range range, out Index start, out Index end)
    {
        start = range.Start;
        end = range.End;
    }

    /// <summary>
    /// Enumerates indexes within the range.
    /// </summary>
    /// <param name="range">The range.</param>
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

    /// <summary>
    /// Projects indexes in the range into a sequence.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="range">The range.</param>
    /// <param name="selector">The selector.</param>
    public static IEnumerable<T> Select<T>(this Range range, Func<int, T> selector)
        => range.AsSequence().Select(selector); 
}
