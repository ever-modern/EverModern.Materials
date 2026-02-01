using System.Buffers;
using System.Collections;

namespace EverModern.WheelProtection.DataStructures.Buffers;

public sealed class BorrowedArray<T> : IDisposable, IReadOnlyList<T>
{
    volatile bool _released = false;

    readonly T[] _array;
    readonly int _length;
    readonly ArrayPool<T> _source;
    int _offset;

    public BorrowedArray(T[] array, int size)
        : this(array, size, ArrayPool<T>.Shared) { }

    public BorrowedArray(T[] array, int size, ArrayPool<T> source)
    {
        _source = source;
        _array = array;
        _length = size;
    }

    public BorrowedArray(ArrayPool<T> source, int size)
        : this(source.Rent(size), size, source) { }

    public BorrowedArray(int size)
        : this(ArrayPool<T>.Shared.Rent(size), size, ArrayPool<T>.Shared) { }

    public BorrowedArray<T> this[Range range]
    {
        get
        {
            var (offset, count) = range.GetOffsetAndLength(_length);
            return new(_array, count, _source) { _offset = offset };
        }
    }

    public T this[int index]
    {
        get
        {
            EnsureAllowedIndex(index);
            return _array[index];
        }
        set
        {
            EnsureAllowedIndex(index);
            _array[index] = value;
        }
    }

    public void Free()
    {
        if (!_released)
        {
            _released = true;
            _source.Return(_array);
        }
    }

    void IDisposable.Dispose() => Free();

    void EnsureAllowedIndex(int i)
    {
        if (i >= _length)
        {
            throw new IndexOutOfRangeException();
        }
    }

    public int Length => _length;

    int IReadOnlyCollection<T>.Count => _length;

    public static implicit operator Span<T>(BorrowedArray<T> borrowedArray) =>
        borrowedArray._array.AsSpan()[borrowedArray._offset..borrowedArray._length];

    public static implicit operator ReadOnlySpan<T>(BorrowedArray<T> borrowedArray) =>
        borrowedArray._array.AsSpan()[borrowedArray._offset..borrowedArray._length];

    public Span<T> AsSpan() => this;

    public ArraySegment<T> AsSegment() => new ArraySegment<T>(_array, _offset, _length);

    public OneArrayWriter<T> GetBufferWriter() => new(AsSegment());

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = _offset; i < _offset + _length; i++)
        {
            yield return _array[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
