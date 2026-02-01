namespace EverModern.WheelProtection.DataStructures.Buffers;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A disposable list implementation that uses BorrowedArray<T> for underlying storage.
/// When disposed, all underlying BorrowedArray<T> instances are disposed as well.
/// </summary>
/// <typeparam name="T">The type of elements in the list</typeparam>
public sealed class BorrowedList<T> : IList<T>, IDisposable, IReadOnlyList<T>
{
    private readonly List<BorrowedArray<T>> _buffers;
    private int _count;
    private bool _disposed;
    private const int DefaultCapacity = 4;

    public BorrowedList()
    {
        _buffers = new List<BorrowedArray<T>>();
        _count = 0;
    }

    public BorrowedList(int capacity)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");

        _buffers = new List<BorrowedArray<T>>();
        if (capacity > 0)
        {
            _buffers.Add(new BorrowedArray<T>(capacity));
        }
        _count = 0;
    }

    public BorrowedList(IEnumerable<T> collection)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        _buffers = new List<BorrowedArray<T>>();
        _count = 0;

        foreach (var item in collection)
        {
            Add(item);
        }
    }

    public T this[int index]
    {
        get
        {
            ThrowIfDisposed();
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return GetItemAt(index);
        }
        set
        {
            ThrowIfDisposed();
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            SetItemAt(index, value);
        }
    }

    public int Count
    {
        get
        {
            ThrowIfDisposed();
            return _count;
        }
    }

    public bool IsReadOnly => false;

    public void Add(T item)
    {
        ThrowIfDisposed();

        EnsureCapacity(_count + 1);
        SetItemAt(_count, item);
        _count++;
    }

    public void Clear()
    {
        ThrowIfDisposed();

        // Dispose all buffers and recreate empty list
        foreach (var buffer in _buffers)
        {
            buffer?.Free();
        }
        _buffers.Clear();
        _count = 0;
    }

    public bool Contains(T item)
    {
        ThrowIfDisposed();
        return IndexOf(item) >= 0;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        if (array.Length - arrayIndex < _count)
            throw new ArgumentException("Destination array is not long enough.");

        for (int i = 0; i < _count; i++)
        {
            array[arrayIndex + i] = GetItemAt(i);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        ThrowIfDisposed();

        for (int i = 0; i < _count; i++)
        {
            yield return GetItemAt(i);
        }
    }

    public int IndexOf(T item)
    {
        ThrowIfDisposed();

        var comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < _count; i++)
        {
            if (comparer.Equals(GetItemAt(i), item))
                return i;
        }
        return -1;
    }

    public void Insert(int index, T item)
    {
        ThrowIfDisposed();

        if (index < 0 || index > _count)
            throw new ArgumentOutOfRangeException(nameof(index));

        EnsureCapacity(_count + 1);

        // Shift elements to the right
        for (int i = _count; i > index; i--)
        {
            SetItemAt(i, GetItemAt(i - 1));
        }

        SetItemAt(index, item);
        _count++;
    }

    public bool Remove(T item)
    {
        ThrowIfDisposed();

        int index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }
        return false;
    }

    public void RemoveAt(int index)
    {
        ThrowIfDisposed();

        if (index < 0 || index >= _count)
            throw new ArgumentOutOfRangeException(nameof(index));

        // Shift elements to the left
        for (int i = index; i < _count - 1; i++)
        {
            SetItemAt(i, GetItemAt(i + 1));
        }

        _count--;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var buffer in _buffers)
            {
                buffer?.Free();
            }
            _buffers.Clear();
            _count = 0;
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BorrowedList<T>));
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        int currentCapacity = _buffers.Sum(b => b?.Length ?? 0);

        if (currentCapacity >= requiredCapacity)
            return;

        int newBufferSize = Math.Max(DefaultCapacity, requiredCapacity - currentCapacity);
        if (_buffers.Count > 0)
        {
            // Double the size for exponential growth
            newBufferSize = Math.Max(newBufferSize, _buffers[^1].Length * 2);
        }

        _buffers.Add(new BorrowedArray<T>(newBufferSize));
    }

    private T GetItemAt(int logicalIndex)
    {
        int currentIndex = 0;
        foreach (var buffer in _buffers)
        {
            if (logicalIndex < currentIndex + buffer.Length)
            {
                return buffer[logicalIndex - currentIndex];
            }
            currentIndex += buffer.Length;
        }
        throw new ArgumentOutOfRangeException(nameof(logicalIndex));
    }

    private void SetItemAt(int logicalIndex, T value)
    {
        int currentIndex = 0;
        foreach (var buffer in _buffers)
        {
            if (logicalIndex < currentIndex + buffer.Length)
            {
                buffer[logicalIndex - currentIndex] = value;
                return;
            }
            currentIndex += buffer.Length;
        }
        throw new ArgumentOutOfRangeException(nameof(logicalIndex));
    }
}
