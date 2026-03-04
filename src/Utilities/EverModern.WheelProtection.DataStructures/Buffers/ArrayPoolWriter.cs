using System.Buffers;

namespace EverModern.WheelProtection.DataStructures.Buffers;

/// <summary>
/// Writes items to inner buffers, which are provided by an ArrayPool.
/// When buffer limit is reached, a new one is borrowed.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public class ArrayPoolWriter<T> : IBufferWriter<T>
{
    const int _defaultExcessMultiplier = 3;

    T[]? _buffer;
    int _index;

    readonly ArrayPool<T> _source;
    readonly int _bufferExcessMultiplier;

    /// <summary>
    /// Initializes a new instance with a custom pool and growth multiplier.
    /// </summary>
    /// <param name="source">The array pool source.</param>
    /// <param name="bufferExcessMultiplier">The growth multiplier.</param>
    public ArrayPoolWriter(ArrayPool<T> source, int bufferExcessMultiplier)
    {
        _source = source;
        _bufferExcessMultiplier = bufferExcessMultiplier;
    }

    /// <summary>
    /// Initializes a new instance using the shared pool.
    /// </summary>
    public ArrayPoolWriter()
        : this(ArrayPool<T>.Shared, _defaultExcessMultiplier)
    {
    }

    int FreeCapacity => _buffer.Length - _index;

    /// <inheritdoc />
    public void Advance(int count)
    {
        if (count < 0)
            throw new ArgumentException(null, nameof(count));

        if (_buffer is null)
        {
            return;
        }

        if (_index > _buffer.Length - count)
            ThrowInvalidOperationException_AdvancedTooFar();

        _index += count;
    }

    /// <inheritdoc />
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_index);
    }

    /// <inheritdoc />
    public Span<T> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_index);
    }

    void EnsureCapacity(int requestedSize)
    {
        if (requestedSize < 0)
            throw new ArgumentException(nameof(requestedSize));

        if (requestedSize == 0)
        {
            requestedSize = 1;
        }

        if (_buffer is null)
        {
            _buffer = _source.Rent(requestedSize * _bufferExcessMultiplier);
            _index = 0;
        }
        else if (requestedSize > FreeCapacity)
        {
            int currentLength = _buffer.Length;

            int growBy = Math.Max(requestedSize, currentLength);

            int newSize = _bufferExcessMultiplier * (currentLength + growBy);

            var temp = _source.Rent(newSize);
            Array.Copy(_buffer, temp, _index);
            _source.Return(_buffer);
            _buffer = temp;
        }
    }

    /// <summary>
    /// Returns the filled part of the underlying buffer and detaches from it.
    /// </summary>
    /// <returns>The borrowed array if any data was written; otherwise <see langword="null"/>.</returns>
    public BorrowedArray<T>? ExtractResults()
    {
        if (_buffer is null)
        {
            return null;
        }

        var buffer = _buffer;
        BorrowedArray<T> result = new(buffer, _index);

        _index = 0;
        _buffer = null;

        return result;
    }

    static void ThrowInvalidOperationException_AdvancedTooFar()
        => throw new InvalidOperationException();
}
