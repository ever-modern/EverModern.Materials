namespace DestallMaterials.WheelProtection.Extensions.Objects;

public static class ObjectExtensions
{
    public static bool IsOneOf<T>(this T item, IEnumerable<T> items)
    {
        return items.Contains(item);
    }

    public static bool IsOneOf<T>(this T item, params T[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].Equals(item))
            {
                return true;
            }
        }
        return false;
    }

    public static IEnumerable<T> Yield<T>(this T item)
    {
        yield return item;
    }

    public static IEnumerable<T> And<T>(this T item1, T item2)
    {
        yield return item1;
        yield return item2;
    }

    public static IEnumerable<T> And<T>(this T item1, IEnumerable<T> items)
    {
        yield return item1;

        foreach (var item in items)
        {
            yield return item;
        }
    }

    public static T[] ToArrayOfOne<T>(this T item)
        => new T[1] { item };
}
