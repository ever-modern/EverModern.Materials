namespace EverModern.WheelProtection.Extensions.HashSets;

/// <summary>
/// Provides helper extensions for hash sets.
/// </summary>
public static class HashSetExtensions
{
    /// <summary>
    /// Adds an item if it does not exist and returns the stored instance.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The set.</param>
    /// <param name="seekedItem">The item to ensure.</param>
    public static T EnsureExists<T>(this HashSet<T> items, T seekedItem)
    {
        if (items.TryGetValue(seekedItem, out var result))
        {
            return result;
        }
        items.Add(seekedItem);
        return seekedItem;
    }
}
