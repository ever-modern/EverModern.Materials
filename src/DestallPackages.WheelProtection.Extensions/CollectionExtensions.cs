using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DestallMaterials.WheelProtection.Extensions.Collections;

public static class CollectionExtensions
{
    public static T WithdrawUntil<T>(this ConcurrentQueue<T> queue, Func<T, bool> condition)
    {
        while (queue.TryDequeue(out var item))
        {
            if (condition(item))
            {
                return item;
            }
        }
        return default;
    }

    public static bool AddIf<TItem, TCollection>(
        this TCollection source,
        TItem item,
        bool condition
    )
        where TCollection : ICollection<TItem>
    {
        if (condition)
        {
            source.Add(item);
            return true;
        }
        return false;
    }

    public static bool AddIfNot<TItem, TCollection>(
        this TCollection source,
        TItem item,
        bool condition
    )
        where TCollection : ICollection<TItem> => source.AddIf(item, !condition);

    /// <summary>
    /// Removes items from collection by a predicate.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    public static void RemoveAll<T>(this ICollection<T> items, Func<T, bool> condition)
    {
        foreach (var item in items.Where(i => condition(i)).ToArray())
        {
            items.Remove(item);
        }
    }

    public static void AddRange<T>(this ICollection<T> items, IEnumerable<T> newItems)
    {
        foreach (var item in newItems)
        {
            items.Add(item);
        }
    }

    public static IReadOnlyList<T> AsReadOnlyList<T>(this IReadOnlyList<T> list) => list;
}
