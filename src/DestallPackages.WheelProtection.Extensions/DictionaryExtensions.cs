using DestallMaterials.WheelProtection.Extensions.Enumerables;
using DestallMaterials.WheelProtection.Extensions.Objects;
using DestallMaterials.WheelProtection.Extensions.SpecialDataTypes;

namespace DestallMaterials.WheelProtection.Extensions.Dictionaries;

public static class DictionaryExtensions
{
    public static Dictionary<TKey, TResult> JoinWithDictionary<TKey, TValue, TResult>(
        this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs,
        IReadOnlyDictionary<TKey, TValue> dictionary,
        Func<TKey, TValue, TValue, TResult> resultSelector
    ) =>
        keyValuePairs.ToDictionary(
            kv => kv.Key,
            kv => resultSelector(kv.Key, kv.Value, dictionary[kv.Key])
        );

    public static MergedDictionary<TKey, TValue> Merge<TKey, TValue>(
        this IEnumerable<IDictionary<TKey, TValue>> dicts
    )
    {
        return new MergedDictionary<TKey, TValue>(dicts);
    }

    public static MergedDictionary<TKey, TValue> Merge<TKey, TValue>(
        this IDictionary<TKey, TValue> dict,
        params IDictionary<TKey, TValue>[] dicts
    )
    {
        return new MergedDictionary<TKey, TValue>(dict.Yield().Union(dicts));
    }

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

    public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(
        this Dictionary<TKey, TValue> dict
    ) => dict;
}
