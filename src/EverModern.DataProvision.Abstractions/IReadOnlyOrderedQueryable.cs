using System.Linq.Expressions;

namespace EverModern.DataProvision.Abstractions;

/// <summary>
/// Ordered read-only LINQ-to-entities query surface with synchronous materialization.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IReadOnlyOrderedQueryable<T> : ISyncMaterializable<T>
{
    /// <summary>
    /// Performs a subsequent ordering by the specified key.
    /// </summary>
    IReadOnlyOrderedQueryable<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector);
    /// <summary>
    /// Performs a subsequent ordering by the specified key in descending order.
    /// </summary>
    IReadOnlyOrderedQueryable<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector);
    /// <summary>
    /// Filters the query with the specified predicate.
    /// </summary>
    IReadOnlyQueryable<T> Where(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Projects each element into a new form.
    /// </summary>
    IReadOnlyQueryable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector);
    /// <summary>
    /// Projects each element to a sequence and flattens the results.
    /// </summary>
    IReadOnlyQueryable<TResult> SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector);
    /// <summary>
    /// Projects each element to a sequence and flattens with a result selector.
    /// </summary>
    IReadOnlyQueryable<TResult> SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector);
    /// <summary>
    /// Groups elements by the specified key.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="keySelector">The key selector.</param>
    IReadOnlyQueryable<IGrouping<TKey, T>> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector);
    /// <summary>
    /// Groups elements by the specified key and projects elements.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="keySelector">The key selector.</param>
    /// <param name="elementSelector">The element selector.</param>
    IReadOnlyQueryable<IGrouping<TKey, TElement>> GroupBy<TKey, TElement>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector);
    /// <summary>
    /// Skips the specified number of elements.
    /// </summary>
    IReadOnlyQueryable<T> Skip(int count);
    /// <summary>
    /// Takes the specified number of elements.
    /// </summary>
    IReadOnlyQueryable<T> Take(int count);
    /// <summary>
    /// Removes duplicate elements.
    /// </summary>
    IReadOnlyQueryable<T> Distinct();
}
