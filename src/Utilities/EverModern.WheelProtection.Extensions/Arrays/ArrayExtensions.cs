namespace EverModern.WheelProtection.Extensions.Arrays;

/// <summary>
/// Provides deconstruction helpers for lists.
/// </summary>
public static class ArrayExtensions
{
    /// <summary>
    /// Deconstructs a list into two items.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="list">The list.</param>
    /// <param name="item1">The first item.</param>
    /// <param name="item2">The second item.</param>
    public static void Deconstruct<T>(this IReadOnlyList<T> list, out T item1, out T item2)
    {
        item1 = list[0];
        item2 = list[1];
    }

    /// <summary>
    /// Deconstructs a list into three items.
    /// </summary>
    public static void Deconstruct<T>(this IReadOnlyList<T> list, out T item1, out T item2, out T item3)
    {
        item1 = list[0];
        item2 = list[1];
        item3 = list[2];
    }

    /// <summary>
    /// Deconstructs a list into four items.
    /// </summary>
    public static void Deconstruct<T>(this IReadOnlyList<T> list, out T item1, out T item2, out T item3, out T item4)
    {
        item1 = list[0];
        item2 = list[1];
        item3 = list[2];
        item4 = list[3];
    }

    /// <summary>
    /// Deconstructs a list into five items.
    /// </summary>
    public static void Deconstruct<T>(this IReadOnlyList<T> list, out T item1, out T item2, out T item3, out T item4, out T item5)
    {
        item1 = list[0];
        item2 = list[1];
        item3 = list[2];
        item4 = list[3];
        item5 = list[4];
    }

    /// <summary>
    /// Deconstructs a list into six items.
    /// </summary>
    public static void Deconstruct<T>(this IReadOnlyList<T> list, out T item1, out T item2, out T item3, out T item4, out T item5, out T item6)
    {
        item1 = list[0];
        item2 = list[1];
        item3 = list[2];
        item4 = list[3];
        item5 = list[4];
        item6 = list[5];
    }

    /// <summary>
    /// Deconstructs a list into seven items.
    /// </summary>
    public static void Deconstruct<T>(this IReadOnlyList<T> list, out T item1, out T item2, out T item3, out T item4, out T item5, out T item6, out T item7)
    {
        item1 = list[0];
        item2 = list[1];
        item3 = list[2];
        item4 = list[3];
        item5 = list[4];
        item6 = list[5];
        item7 = list[6];
    }

    /// <summary>
    /// Deconstructs a list into eight items.
    /// </summary>
    public static void Deconstruct<T>(this IReadOnlyList<T> list, out T item1, out T item2, out T item3, out T item4, out T item5, out T item6, out T item7, out T item8)
    {
        item1 = list[0];
        item2 = list[1];
        item3 = list[2];
        item4 = list[3];
        item5 = list[4];
        item6 = list[5];
        item7 = list[6];
        item8 = list[7];
    }

    /// <summary>
    /// Deconstructs a list into nine items.
    /// </summary>
    public static void Deconstruct<T>(this IReadOnlyList<T> list, out T item1, out T item2, out T item3, out T item4, out T item5, out T item6, out T item7, out T item8, out T item9)
    {
        item1 = list[0];
        item2 = list[1];
        item3 = list[2];
        item4 = list[3];
        item5 = list[4];
        item6 = list[5];
        item7 = list[6];
        item8 = list[7];
        item9 = list[8];
    }

    /// <summary>
    /// Deconstructs a list into ten items.
    /// </summary>
    public static void Deconstruct<T>(this IReadOnlyList<T> list, out T item1, out T item2, out T item3, out T item4, out T item5, out T item6, out T item7, out T item8, out T item9, out T item10)
    {
        item1 = list[0];
        item2 = list[1];
        item3 = list[2];
        item4 = list[3];
        item5 = list[4];
        item6 = list[5];
        item7 = list[6];
        item8 = list[7];
        item9 = list[8];
        item10 = list[9];
    }
}