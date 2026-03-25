namespace EverModern.WheelProtection.Extensions.Objects;

/// <summary>
/// Provides convenience extensions for objects.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    /// Yields the item as a single-element sequence.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="item">The item.</param>
    public static IEnumerable<T> Yield<T>(this T item)
    {
        yield return item;
    }

    /// <summary>
    /// Yields two items as a sequence.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="item1">The first item.</param>
    /// <param name="item2">The second item.</param>
    public static IEnumerable<T> And<T>(this T item1, T item2)
    {
        yield return item1;
        yield return item2;
    }

    /// <summary>
    /// Concatenates an item with a sequence.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="item1">The first item.</param>
    /// <param name="items">The remaining items.</param>
    public static IEnumerable<T> And<T>(this T item1, IEnumerable<T> items)
    {
        yield return item1;

        foreach (var item in items)
        {
            yield return item;
        }
    }

    /// <summary>
    /// Wraps the item into a single-element array.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="item">The item.</param>
    public static T[] ToArrayOfOne<T>(this T item)
        => new T[1] { item };
}
