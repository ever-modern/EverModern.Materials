using System.Buffers;

namespace EverModern.WheelProtection.DataStructures.Buffers;

/// <summary>
/// Factory for array-backed buffer writers.
/// </summary>
public abstract class OneArrayWriter
{
    /// <summary>
    /// Creates an array-backed buffer writer.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="arr">The backing array.</param>
    public static OneArrayWriter<T> Create<T>(T[] arr)
        => new(arr);
}

/// <summary>
/// Buffer writer backed by a single array segment.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public class OneArrayWriter<T> : IBufferWriter<T>
{
    readonly ArraySegment<T> _inner;
    int _index;

    /// <summary>
    /// Initializes a new instance with the specified segment.
    /// </summary>
    /// <param name="inner">The backing segment.</param>
    public OneArrayWriter(ArraySegment<T> inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Initializes a new instance with the specified array.
    /// </summary>
    /// <param name="array">The backing array.</param>
    public OneArrayWriter(T[] array)
        : this(new ArraySegment<T>(array))
    {
    }

    /// <inheritdoc />
    public void Advance(int count)
    {
        _index += count;

        if (_index > _inner.Count)
        {
            throw new IndexOutOfRangeException("Resultant index was outside the underlying array.");
        }
    }

    /// <inheritdoc />
    public Memory<T> GetMemory(int sizeHint = 0)
        => GetSegment(sizeHint);

    /// <inheritdoc />
    public Span<T> GetSpan(int sizeHint = 0)
        => GetSegment(sizeHint);

    ArraySegment<T> GetSegment(int sizeRequested)
    {
        if (_index + sizeRequested > _inner.Count)
        {
            throw new IndexOutOfRangeException("Not enough memory in the array to spare.");
        }
        var result = _inner[_index..(_index + sizeRequested)];
        return result;
    }
}