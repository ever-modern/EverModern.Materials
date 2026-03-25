using System.Collections;

namespace EverModern.WheelProtection.DataStructures;

/// <summary>
/// List implementation that disposes items when disposed.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
public class DisposableList<TItem> : IList<TItem>, IDisposable
    where TItem : IDisposable
{
    readonly List<TItem> _items = [];

    /// <inheritdoc />
    public TItem this[int index] { get => _items[index]; set => _items[index] = value; }

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public void Add(TItem item)
    {
        _items.Add(item);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _items.Clear();
    }

    /// <inheritdoc />
    public bool Contains(TItem item)
    {
        return _items.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(TItem[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var item in _items)
        {
            item.Dispose();
        }
    }

    /// <inheritdoc />
    public IEnumerator<TItem> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    /// <inheritdoc />
    public int IndexOf(TItem item)
    {
        return _items.IndexOf(item);
    }

    /// <inheritdoc />
    public void Insert(int index, TItem item)
    {
        _items.Insert(index, item);
    }

    /// <inheritdoc />
    public bool Remove(TItem item)
    {
        return _items.Remove(item);
    }

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        _items.RemoveAt(index);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    /// <summary>
    /// Adds items that are not already present.
    /// </summary>
    /// <param name="items">The items to add.</param>
    public void AddRange(IEnumerable<TItem> items)
    {
        foreach (var item in items.Where(i => !_items.Contains(i)))
        {
            _items.Add(item);
        }
    }
}