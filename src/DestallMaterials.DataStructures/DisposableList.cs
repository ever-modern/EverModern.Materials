using System.Collections;

namespace DestallMaterials.WheelProtection.DataStructures;

public class DisposableList<TItem> : IList<TItem>, IDisposable
    where TItem : IDisposable
{
    readonly List<TItem> _items = new List<TItem>();

    public TItem this[int index] { get => _items[index]; set => _items[index] = value; }

    public int Count => _items.Count;

    public bool IsReadOnly => false;

    public void Add(TItem item)
    {
        _items.Add(item);
    }

    public void Clear()
    {
        _items.Clear();
    }

    public bool Contains(TItem item)
    {
        return _items.Contains(item);
    }

    public void CopyTo(TItem[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    public void Dispose()
    {
        foreach (var item in _items)
        {
            item.Dispose();
        }
    }

    public IEnumerator<TItem> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    public int IndexOf(TItem item)
    {
        return _items.IndexOf(item);
    }

    public void Insert(int index, TItem item)
    {
        _items.Insert(index, item);
    }

    public bool Remove(TItem item)
    {
        return _items.Remove(item);
    }

    public void RemoveAt(int index)
    {
        _items.RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    public void AddRange(IEnumerable<TItem> items)
    {
        foreach (var item in items.Where(i => !_items.Contains(i)))
        {
            _items.Add(item);
        }
    }
}