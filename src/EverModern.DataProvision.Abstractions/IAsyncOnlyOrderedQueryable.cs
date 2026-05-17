using System.Linq.Expressions;

namespace EverModern.DataProvision.Abstractions;

/// <summary>
/// Ordered LINQ-to-entities query surface with asynchronous materialization.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IAsyncOnlyOrderedQueryable<T> : IAsyncMaterializable<T>
{
    /// <summary>
    /// Performs a subsequent ordering by the specified key.
    /// </summary>
    IAsyncOnlyOrderedQueryable<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector);
    /// <summary>
    /// Performs a subsequent ordering by the specified key in descending order.
    /// </summary>
    IAsyncOnlyOrderedQueryable<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector);
    /// <summary>
    /// Filters the query with the specified predicate.
    /// </summary>
    IAsyncOnlyQueryable<T> Where(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Projects each element into a new form.
    /// </summary>
    IAsyncOnlyQueryable<TResult> Select<TResult>(Expression<Func<T, TResult>> selector);
    /// <summary>
    /// Projects each element to a sequence and flattens the results.
    /// </summary>
    IAsyncOnlyQueryable<TResult> SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector);
    /// <summary>
    /// Projects each element to a sequence and flattens with a result selector.
    /// </summary>
    IAsyncOnlyQueryable<TResult> SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector);
    /// <summary>
    /// Groups elements by the specified key.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="keySelector">The key selector.</param>
    IAsyncOnlyQueryable<IGrouping<TKey, T>> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector);
    /// <summary>
    /// Groups elements by the specified key and projects elements.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="keySelector">The key selector.</param>
    /// <param name="elementSelector">The element selector.</param>
    IAsyncOnlyQueryable<IGrouping<TKey, TElement>> GroupBy<TKey, TElement>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector);
    /// <summary>
    /// Correlates elements of two sequences based on key equality and groups matching inner elements.
    /// </summary>
    IAsyncOnlyQueryable<TResult> GroupJoin<TInner, TKey, TResult>(
        IAsyncOnlyOrderedQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector);
    /// <summary>
    /// Correlates elements of two sequences based on key equality and keeps all elements from the outer sequence.
    /// </summary>
    IAsyncOnlyQueryable<TResult> LeftJoin<TInner, TKey, TResult>(
        IAsyncOnlyOrderedQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector);
    /// <summary>
    /// Skips the specified number of elements.
    /// </summary>
    IAsyncOnlyQueryable<T> Skip(int count);
    /// <summary>
    /// Takes the specified number of elements.
    /// </summary>
    IAsyncOnlyQueryable<T> Take(int count);
    /// <summary>
    /// Removes duplicate elements.
    /// </summary>
    IAsyncOnlyQueryable<T> Distinct();
}
