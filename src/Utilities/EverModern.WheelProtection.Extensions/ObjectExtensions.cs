namespace EverModern.WheelProtection.Extensions.Objects;

public static class ObjectExtensions
{
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
