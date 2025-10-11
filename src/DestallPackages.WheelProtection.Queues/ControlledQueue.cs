using DestallMaterials.WheelProtection.DataStructures;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

namespace DestallMaterials.WheelProtection.Queues;

class ControlledQueue<T> where T : class
{
    readonly object _locker = new object();
    readonly List<T> _items = [];
    readonly Func<T, bool> _filter;

    public ControlledQueue(Func<T, bool> filter)
    {
        _filter = filter;
    }

    public bool TryDequeue(out T result)
    {
        var filter = _filter;
        lock (_locker)
        {
            var l = _items.Count;
            for (int i = 0; i < l; i++)
            {
                result = _items[i];

                _items.RemoveAt(i);
                i--;
                l--;

                var fits = filter(result);
                if (fits)
                {
                    return true;
                }
            }

            result = null;
            return false;
        }
    }

    public void Enqueue(T item)
    {
        lock (_locker)
        {
            _items.Add(item);
        }
    }

    public T? FindThrough()
    {
        var filter = _filter;
        lock (_locker)
        {
            var l = _items.Count;
            for (int i = 0; i < l; i++)
            {
                var result = _items[i];
                var fits = filter(result);
                if (fits)
                {
                    return result;
                }

                _items.RemoveAt(i);
                i--;
                l--;
            }

            return null;
        }
    }
}