using System.Buffers;
using System.Collections;

namespace EverModern.WheelProtection.DataStructures.Buffers;

/// <summary>
/// Represents an array rented from an array pool.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class BorrowedArray<T> : IDisposable, IReadOnlyList<T>
{
    volatile bool _released = false;

    readonly T[] _array;
    readonly int _length;
    readonly ArrayPool<T> _source;
    int _offset;

    /// <summary>
    /// Initializes a new instance with an existing array.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <param name="size">The logical size.</param>
    public BorrowedArray(T[] array, int size)
        : this(array, size, ArrayPool<T>.Shared) { }

    /// <summary>
    /// Initializes a new instance with an existing array and pool.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <param name="size">The logical size.</param>
    /// <param name="source">The array pool source.</param>
    public BorrowedArray(T[] array, int size, ArrayPool<T> source)
    {
        _source = source;
        _array = array;
        _length = size;
    }

    /// <summary>
    /// Initializes a new instance by renting from a pool.
    /// </summary>
    /// <param name="source">The array pool source.</param>
    /// <param name="size">The logical size.</param>
    public BorrowedArray(ArrayPool<T> source, int size)
        : this(source.Rent(size), size, source) { }

    /// <summary>
    /// Initializes a new instance by renting from the shared pool.
    /// </summary>
    /// <param name="size">The logical size.</param>
    public BorrowedArray(int size)
        : this(ArrayPool<T>.Shared.Rent(size), size, ArrayPool<T>.Shared) { }

    /// <summary>
    /// Slices the borrowed array.
    /// </summary>
    /// <param name="range">The range to slice.</param>
    public BorrowedArray<T> this[Range range]
    {
        get
        {
            var (offset, count) = range.GetOffsetAndLength(_length);
            return new(_array, count, _source) { _offset = offset };
        }
    }

    /// <inheritdoc />
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

    /// <summary>
    /// Returns the array to the pool.
    /// </summary>
    public void Free()
    {
        if (!_released)
        {
            _released = true;
            _source.Return(_array);
        }
    }

    /// <inheritdoc />
    void IDisposable.Dispose() => Free();

    void EnsureAllowedIndex(int i)
    {
        if (i >= _length)
        {
            throw new IndexOutOfRangeException();
        }
    }

    /// <summary>
    /// Gets the logical length.
    /// </summary>
    public int Length => _length;

    /// <inheritdoc />
    int IReadOnlyCollection<T>.Count => _length;

    /// <summary>
    /// Converts to a span over the borrowed range.
    /// </summary>
    public static implicit operator Span<T>(BorrowedArray<T> borrowedArray) =>
        borrowedArray._array.AsSpan()[borrowedArray._offset..borrowedArray._length];

    /// <summary>
    /// Converts to a read-only span over the borrowed range.
    /// </summary>
    public static implicit operator ReadOnlySpan<T>(BorrowedArray<T> borrowedArray) =>
        borrowedArray._array.AsSpan()[borrowedArray._offset..borrowedArray._length];

    /// <summary>
    /// Gets a span over the borrowed range.
    /// </summary>
    public Span<T> AsSpan() => this;

    /// <summary>
    /// Gets an array segment over the borrowed range.
    /// </summary>
    public ArraySegment<T> AsSegment() => new ArraySegment<T>(_array, _offset, _length);

    /// <summary>
    /// Gets a buffer writer backed by the borrowed range.
    /// </summary>
    public OneArrayWriter<T> GetBufferWriter() => new(AsSegment());

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = _offset; i < _offset + _length; i++)
        {
            yield return _array[i];
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
