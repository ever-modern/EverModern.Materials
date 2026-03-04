using EverModern.WheelProtection.Extensions.Tasks;
using System.Collections;

namespace EverModern.WheelProtection.Extensions.Enumerables;

/// <summary>
/// Provides helper extensions for enumerable sequences.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Projects items to tasks and yields results as they complete.
    /// </summary>
    /// <typeparam name="T">The input type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="items">The items.</param>
    /// <param name="selector">The async selector.</param>
    public static IAsyncEnumerable<TResult> WhenEachAsync<T, TResult>(
        this IEnumerable<T> items,
        Func<T, Task<TResult>> selector
    ) => items.Select(selector).WhenEach();

    /// <summary>
    /// Yields task results as tasks complete.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="tasks">The tasks.</param>
    public static async IAsyncEnumerable<T> WhenEach<T>(
        this IEnumerable<Task<T>> tasks
    )
    {
        var tasksList = tasks.ToList();
        while (tasksList.Count > 0)
        {
            await Task.WhenAny(tasksList);
            var completedTasks = tasksList.Where(t => t.IsCompleted).ToArray();
            for (int i = 0; i < completedTasks.Length; i++)
            {
                var completedTask = completedTasks[i];
                tasksList.Remove(completedTask);
                yield return completedTask.Result;
            }
        }
    }

    /// <summary>
    /// Awaits all tasks and projects the results.
    /// </summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <typeparam name="TResult">The projected type.</typeparam>
    /// <param name="items">The tasks.</param>
    /// <param name="selector">The projection.</param>
    public static Task<TResult> WhenAll<T, TResult>(
        this IEnumerable<Task<T>> items,
        Func<T[], TResult> selector
    ) => Task.WhenAll(items).Then(results => selector(results));

    /// <summary>
    /// Awaits all tasks.
    /// </summary>
    /// <param name="items">The tasks.</param>
    public static Task WhenAll(
        this IEnumerable<Task> items
    ) => Task.WhenAll(items);

    /// <summary>
    /// Awaits all tasks.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="items">The tasks.</param>
    public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> items) =>
        Task.WhenAll(items);

    /// <summary>
    /// Builds a dictionary with async key selection.
    /// </summary>
    public static async Task<Dictionary<TKey, TValue>> ToDictionaryAsync<TIn, TKey, TValue>(
        this IEnumerable<TIn> ins,
        Func<TIn, Task<TKey>> asyncKeySelector,
        Func<TIn, TValue> valueSelector
    ) where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>();

        await foreach (
            var item in ins.WhenEachAsync(
                async i => new { Key = await asyncKeySelector(i), Value = valueSelector(i) }
            )
        )
        {
            result.Add(item.Key, item.Value);
        }

        return result;
    }

    /// <summary>
    /// Builds a dictionary with async value selection.
    /// </summary>
    public static async Task<Dictionary<TKey, TValue>> ToDictionaryAsync<TIn, TKey, TValue>(
        this IEnumerable<TIn> ins,
        Func<TIn, TKey> keySelector,
        Func<TIn, Task<TValue>> asyncValueSelector
    ) where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>();

        await foreach (
            var item in ins.WhenEachAsync(
                async i => new { Key = keySelector(i), Value = await asyncValueSelector(i) }
            )
        )
        {
            result.Add(item.Key ?? throw new ArgumentNullException(), item.Value);
        }

        return result;
    }

    /// <summary>
    /// Builds a dictionary with async key/value selection.
    /// </summary>
    public static async Task<Dictionary<TKey, TValue>> ToDictionaryAsync<TIn, TKey, TValue>(
        this IEnumerable<TIn> ins,
        Func<TIn, Task<TKey>> asyncKeySelector,
        Func<TIn, Task<TValue>> asyncValueSelector
    ) where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>();

        await foreach (
            var item in ins.WhenEachAsync(async i =>
            {
                var keyTask = asyncKeySelector(i);
                var valueTask = asyncValueSelector(i);
                var result = new { Key = await keyTask, Value = await valueTask };
                return result;
            })
        )
        {
            result.Add(item.Key, item.Value);
        }

        return result;
    }

    class InnerEnumerable<T> : IEnumerable<T>
    {
        readonly IEnumerator<T> _generalEnumerator;
        readonly int _limit;
        readonly T _firstItem;

        public InnerEnumerable(IEnumerator<T> generalEnumerator, int limit, T firstItem)
        {
            _generalEnumerator = generalEnumerator;
            _limit = limit;
            _firstItem = firstItem;
        }

        public IEnumerator<T> GetEnumerator()
        {
            int i = 0;
            if (i++ == 0)
            {
                yield return _firstItem;
            }
            while (i++ < _limit && _generalEnumerator.MoveNext())
            {
                yield return _generalEnumerator.Current;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Splits a sequence into chunks.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="chunkSize">The chunk size.</param>
    public static IEnumerable<IEnumerable<T>> Split<T>(
        this IEnumerable<T> source,
        int chunkSize
    )
    {
        var generalEnumerator = source.GetEnumerator();

        while (generalEnumerator.MoveNext())
        {
            var enumerable = new InnerEnumerable<T>(
                generalEnumerator,
                chunkSize,
                generalEnumerator.Current
            );
            yield return enumerable;
        }
    }

    /// <summary>
    /// Executes an action for each item.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="action">The action.</param>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }

    /// <summary>
    /// Determines whether the sequence has any items.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The sequence.</param>
    public static bool HasContent<T>(this IEnumerable<T>? items) => items?.Any() == true;

    /// <summary>
    /// Determines whether the sequence is empty.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The sequence.</param>
    public static bool IsEmpty<T>(this IEnumerable<T>? items) => !items.HasContent();

    /// <summary>
    /// Filters items that do not match a condition.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The source sequence.</param>
    /// <param name="condition">The predicate to negate.</param>
    public static IEnumerable<T> WhereNot<T>(
        this IEnumerable<T> items,
        Func<T, bool> condition
    ) => items.Where(i => !condition(i));

    /// <summary>
    /// Returns distinct items using a custom equality predicate.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="areSame">The equality predicate.</param>
    public static IEnumerable<T> DistinctBy<T>(
        this IEnumerable<T> source,
        Func<T, T, bool> areSame
    )
    {
        List<T> items = [];
        foreach (var item in source)
        {
            if (!items.Any(i => areSame(i, item)))
            {
                items.Add(item);
                yield return item;
            }
        }
    }

    /// <summary>
    /// Determines whether the item is in the sequence.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="item">The item.</param>
    /// <param name="items">The items to check.</param>
    public static bool IsOneOf<T>(this T item, IEnumerable<T> items) => items.Contains(item);

    /// <summary>
    /// Determines whether the item is in the set.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="item">The item.</param>
    /// <param name="items">The items to check.</param>
    public static bool IsOneOf<T>(this T item, params T[] items) => items.Contains(item);

    /// <summary>
    /// Orders task results by a selector.
    /// </summary>
    public static Task<IOrderedEnumerable<T>> OrderByAsync<T, TSelector>(
        this IEnumerable<Task<T>> source,
        Func<T, TSelector> selector
    ) => Task.WhenAll(source).Then(items => items.OrderBy(selector));

    /// <summary>
    /// Orders task results by a selector in descending order.
    /// </summary>
    public static Task<IOrderedEnumerable<T>> OrderByDescendingAsync<T, TSelector>(
        this IEnumerable<Task<T>> source,
        Func<T, TSelector> selector
    ) => Task.WhenAll(source).Then(items => items.OrderByDescending(selector));

    /// <summary>
    /// Materializes a sequence into a read-only list.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The source sequence.</param>
    public static IReadOnlyList<T> EnsureMaterialized<T>(this IEnumerable<T> source) =>
        source as IReadOnlyList<T> ?? [.. source];

    /// <summary>
    /// Finds the index of the first item matching a predicate.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The source sequence.</param>
    /// <param name="stopWhen">The predicate.</param>
    public static int FindIndex<T>(this IEnumerable<T> items, Func<T, bool> stopWhen)
    {
        int i = 0;
        foreach (var item in items)
        {
            if (stopWhen(item))
            {
                return i;
            }
            i++;
        }
        return -1;
    }

    /// <summary>
    /// Determines whether no items match a predicate.
    /// </summary>
    public static bool None<T>(this IEnumerable<T> source, Func<T, bool> condition)
        => !source.Any(condition);

    /// <summary>
    /// Determines whether a sequence has no items.
    /// </summary>
    public static bool None<T>(this IEnumerable<T> source)
        => !source.Any();

    /// <summary>
    /// Splits a sequence into groups separated by items matching a condition.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The source sequence.</param>
    /// <param name="splitCondition">The split condition.</param>
    public static IEnumerable<List<T>> SplitBy<T>(this IEnumerable<T> items, Func<T, bool> splitCondition)
    {
        var result = new List<T>();
        bool foundValid = false;
        foreach (var item in items)
        {
            if (splitCondition(item) is true)
            {
                if (foundValid)
                {
                    yield return result;
                    foundValid = false;
                }
            }
            else
            {
                if (foundValid is false)
                {
                    result = [];
                    foundValid = true;
                }

                result.Add(item);
            }
        }

        if (foundValid)
        {
            yield return result;
        }
    }
}