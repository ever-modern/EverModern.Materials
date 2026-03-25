using EverModern.WheelProtection.Extensions.Objects;
using EverModern.WheelProtection.Extensions.SpecialDataTypes;

namespace EverModern.WheelProtection.Extensions.Enumerables;

/// <summary>
/// Provides dictionary extension helpers.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Joins key/value pairs with another dictionary and projects the result.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="keyValuePairs">The key/value pairs.</param>
    /// <param name="dictionary">The dictionary to join.</param>
    /// <param name="resultSelector">The result selector.</param>
    public static Dictionary<TKey, TResult> JoinWithDictionary<TKey, TValue, TResult>(
        this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs,
        IReadOnlyDictionary<TKey, TValue> dictionary,
        Func<TKey, TValue, TValue, TResult> resultSelector
    ) =>
        keyValuePairs.ToDictionary(
            kv => kv.Key,
            kv => resultSelector(kv.Key, kv.Value, dictionary[kv.Key])
        );

    /// <summary>
    /// Merges multiple dictionaries into a merged view.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="dicts">The dictionaries to merge.</param>
    public static MergedDictionary<TKey, TValue> Merge<TKey, TValue>(
        this IEnumerable<IDictionary<TKey, TValue>> dicts
    )
    {
        return new MergedDictionary<TKey, TValue>(dicts);
    }

    /// <summary>
    /// Merges the dictionary with others into a merged view.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="dict">The primary dictionary.</param>
    /// <param name="dicts">The additional dictionaries.</param>
    public static MergedDictionary<TKey, TValue> Merge<TKey, TValue>(
        this IDictionary<TKey, TValue> dict,
        params IDictionary<TKey, TValue>[] dicts
    )
    {
        return new MergedDictionary<TKey, TValue>(dict.Yield().Union(dicts));
    }

    /// <summary>
    /// Ensures keys are present in the dictionary.
    /// </summary>
    /// <typeparam name="TDictionary">The dictionary type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="dict">The dictionary.</param>
    /// <param name="keys">The keys to ensure.</param>
    /// <param name="valueGenerator">The value generator.</param>
    public static TDictionary EnsureKeysArePresent<TDictionary, TKey, TValue>(
        this TDictionary dict,
        IEnumerable<TKey> keys,
        Func<TKey, TValue> valueGenerator
    )
        where TDictionary : IDictionary<TKey, TValue>
    {
        var absentKeys = keys.WhereNot(dict.ContainsKey).Distinct();
        foreach (var key in absentKeys)
        {
            dict.Add(key, valueGenerator(key));
        }

        return dict;
    }

    /// <summary>
    /// Removes all items matching a condition.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="condition">The removal condition.</param>
    public static int RemoveAll<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        Func<KeyValuePair<TKey, TValue>, bool> condition
    )
    {
        int result = 0;
        foreach (var key in dictionary.Where(condition).Select(kv => kv.Key).ToArray())
        {
            dictionary.Remove(key);
            result++;
        }
        return result;
    }

    /// <summary>
    /// Returns a read-only view of the dictionary.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="dict">The dictionary.</param>
    public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(
        this Dictionary<TKey, TValue> dict
    ) => dict;
}
