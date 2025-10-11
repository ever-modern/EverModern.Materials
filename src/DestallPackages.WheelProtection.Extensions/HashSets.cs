namespace DestallMaterials.WheelProtection.Extensions.HashSets;

public static class HashSetExtensions
{
    /// <summary>
    /// Adds item to the set and returns added item if it didn't exist. If existed, the existing item will be returned.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="seekedItem"></param>
    /// <returns></returns>
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
