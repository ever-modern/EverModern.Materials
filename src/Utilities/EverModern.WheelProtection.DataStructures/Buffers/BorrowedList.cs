namespace EverModern.WheelProtection.DataStructures.Buffers;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A disposable list implementation that uses <see cref="BorrowedArray{T}"/> for underlying storage.
/// When disposed, all underlying borrowed arrays are disposed as well. This type is thread-safe.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public sealed class BorrowedList<T> : IList<T>, IDisposable, IReadOnlyList<T>
{
    private readonly List<BorrowedArray<T>> _buffers;
    private int _count;
    private bool _disposed;
    private readonly object _sync = new();
    private const int DefaultCapacity = 4;

    /// <summary>
    /// Initializes an empty list.
    /// </summary>
    public BorrowedList()
    {
        _buffers = [];
        _count = 0;
    }

    /// <summary>
    /// Initializes a list with an initial capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
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

    /// <summary>
    /// Initializes a list with items from a collection.
    /// </summary>
    /// <param name="collection">The source collection.</param>
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

    /// <inheritdoc />
    public T this[int index]
    {
        get
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return GetItemAt(index);
            }
        }
        set
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                SetItemAt(index, value);
            }
        }
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                return _count;
            }
        }
    }

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public void Add(T item)
    {
        lock (_sync)
        {
            ThrowIfDisposed();

            EnsureCapacity(_count + 1);
            SetItemAt(_count, item);
            _count++;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_sync)
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
    }

    /// <inheritdoc />
    public bool Contains(T item)
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            return IndexOf(item) >= 0;
        }
    }

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (_sync)
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
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        T[] snapshot;
        lock (_sync)
        {
            ThrowIfDisposed();
            snapshot = new T[_count];
            for (int i = 0; i < _count; i++)
            {
                snapshot[i] = GetItemAt(i);
            }
        }

        return ((IEnumerable<T>)snapshot).GetEnumerator();
    }

    /// <inheritdoc />
    public int IndexOf(T item)
    {
        lock (_sync)
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
    }

    /// <inheritdoc />
    public void Insert(int index, T item)
    {
        lock (_sync)
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
    }

    /// <inheritdoc />
    public bool Remove(T item)
    {
        lock (_sync)
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
    }

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        lock (_sync)
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
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync)
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
    }

    public void AddRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        lock (_sync)
        {
            ThrowIfDisposed();

            IEnumerable<T> sourceItems = items;
            int? count = items switch
            {
                ICollection<T> collection => collection.Count,
                IReadOnlyCollection<T> readOnlyCollection => readOnlyCollection.Count,
                ICollection nonGenericCollection => nonGenericCollection.Count,
                _ => null
            };

            if (ReferenceEquals(items, this))
            {
                var snapshot = new T[_count];
                for (int i = 0; i < _count; i++)
                {
                    snapshot[i] = GetItemAt(i);
                }
                sourceItems = snapshot;
                count = snapshot.Length;
            }

            if (count.HasValue && count.Value > 0)
            {
                EnsureCapacity(_count + count.Value);
            }

            foreach (var item in sourceItems)
            {
                EnsureCapacity(_count + 1);
                SetItemAt(_count, item);
                _count++;
            }
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
