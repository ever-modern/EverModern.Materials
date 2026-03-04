using System.Collections.Concurrent;

namespace EverModern.WheelProtection.Extensions.Enumerables;

/// <summary>
/// Provides collection helper extensions.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Dequeues items until the condition matches.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="queue">The queue.</param>
    /// <param name="condition">The match condition.</param>
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

    /// <summary>
    /// Adds an item if the condition is true.
    /// </summary>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    /// <param name="source">The collection.</param>
    /// <param name="item">The item.</param>
    /// <param name="condition">The condition.</param>
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

    /// <summary>
    /// Adds an item if the condition is false.
    /// </summary>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    /// <param name="source">The collection.</param>
    /// <param name="item">The item.</param>
    /// <param name="condition">The condition.</param>
    public static bool AddIfNot<TItem, TCollection>(
        this TCollection source,
        TItem item,
        bool condition
    )
        where TCollection : ICollection<TItem> => source.AddIf(item, !condition);

    /// <summary>
    /// Removes items from a collection by predicate.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The collection.</param>
    /// <param name="condition">The removal condition.</param>
    public static void RemoveAll<T>(this ICollection<T> items, Func<T, bool> condition)
    {
        foreach (var item in items.Where(i => condition(i)).ToArray())
        {
            items.Remove(item);
        }
    }

    /// <summary>
    /// Adds a range of items to the collection.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The collection.</param>
    /// <param name="newItems">The items to add.</param>
    public static void AddRange<T>(this ICollection<T> items, IEnumerable<T> newItems)
    {
        foreach (var item in newItems)
        {
            items.Add(item);
        }
    }

    /// <summary>
    /// Returns the list as a read-only list.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="list">The list.</param>
    public static IReadOnlyList<T> AsReadOnlyList<T>(this IReadOnlyList<T> list) => list;
}
