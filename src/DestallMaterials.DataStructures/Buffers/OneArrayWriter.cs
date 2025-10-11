using System.Buffers;

namespace DestallMaterials.WheelProtection.DataStructures.Buffers;

public abstract class OneArrayWriter
{
    public static OneArrayWriter<T> Create<T>(T[] arr)
        => new OneArrayWriter<T>(arr);
}

public class OneArrayWriter<T> : IBufferWriter<T>
{
    readonly ArraySegment<T> _inner;
    int _index;

    public OneArrayWriter(ArraySegment<T> inner)
    {
        _inner = inner;
    }

    public OneArrayWriter(T[] array)
        : this(new ArraySegment<T>(array))
    {
    }

    public void Advance(int count)
    {
        _index += count;

        if (_index > _inner.Count)
        {
            throw new IndexOutOfRangeException("Resultant index was outside the underlying array.");
        }
    }

    public Memory<T> GetMemory(int sizeHint = 0)
        => GetSegment(sizeHint);

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