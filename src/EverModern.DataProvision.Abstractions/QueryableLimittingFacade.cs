using System.Linq.Expressions;

namespace EverModern.DataProvision.Abstractions;

/// <summary>
/// Wraps an <see cref="IQueryable{T}"/> and exposes limited LINQ-to-entities surfaces via
/// explicit interface implementations.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public class QueryableLimittingFacade<T>(IQueryable<T> source) : 
    IReadOnlyQueryable<T>, IReadOnlyOrderedQueryable<T>
{
    readonly IQueryable<T> _source = source;

    /// <inheritdoc/>
    public Expression Expression => _source.Expression;

    QueryableLimittingFacade<TResult> Wrap<TResult>(IQueryable<TResult> query)
        => new(query);

    List<T> ISyncMaterializable<T>.ToList()
        => _source.ToList();

    T[] ISyncMaterializable<T>.ToArray()
        => _source.ToArray();

    Dictionary<TKey, T> ISyncMaterializable<T>.ToDictionary<TKey>(Func<T, TKey> keySelector)
        => _source.ToDictionary(keySelector);

    Dictionary<TKey, TElement> ISyncMaterializable<T>.ToDictionary<TKey, TElement>(
        Func<T, TKey> keySelector,
        Func<T, TElement> elementSelector)
        => _source.ToDictionary(keySelector, elementSelector);

    T ISyncMaterializable<T>.First()
        => _source.First();

    T ISyncMaterializable<T>.First(Expression<Func<T, bool>> predicate)
        => _source.First(predicate);

    T? ISyncMaterializable<T>.FirstOrDefault()
        => _source.FirstOrDefault();

    T? ISyncMaterializable<T>.FirstOrDefault(Expression<Func<T, bool>> predicate)
        => _source.FirstOrDefault(predicate);

    T ISyncMaterializable<T>.Single()
        => _source.Single();

    T ISyncMaterializable<T>.Single(Expression<Func<T, bool>> predicate)
        => _source.Single(predicate);

    T? ISyncMaterializable<T>.SingleOrDefault()
        => _source.SingleOrDefault();

    T? ISyncMaterializable<T>.SingleOrDefault(Expression<Func<T, bool>> predicate)
        => _source.SingleOrDefault(predicate);

    bool ISyncMaterializable<T>.Any()
        => _source.Any();

    bool ISyncMaterializable<T>.Any(Expression<Func<T, bool>> predicate)
        => _source.Any(predicate);

    int ISyncMaterializable<T>.Count()
        => _source.Count();

    int ISyncMaterializable<T>.Count(Expression<Func<T, bool>> predicate)
        => _source.Count(predicate);

    long ISyncMaterializable<T>.LongCount()
        => _source.LongCount();

    long ISyncMaterializable<T>.LongCount(Expression<Func<T, bool>> predicate)
        => _source.LongCount(predicate);

    decimal ISyncMaterializable<T>.Sum(Expression<Func<T, decimal>> selector)
        => _source.Sum(selector);

    decimal ISyncMaterializable<T>.Average(Expression<Func<T, decimal>> selector)
        => _source.Average(selector);

    TResult ISyncMaterializable<T>.Min<TResult>(Expression<Func<T, TResult>> selector)
        => _source.Min(selector);

    TResult ISyncMaterializable<T>.Max<TResult>(Expression<Func<T, TResult>> selector)
        => _source.Max(selector);

    // IReadOnlyQueryable<T> implementations
    IReadOnlyQueryable<T> IReadOnlyQueryable<T>.Where(Expression<Func<T, bool>> predicate)
        => Wrap(_source.Where(predicate));

    IReadOnlyQueryable<TResult> IReadOnlyQueryable<T>.Select<TResult>(Expression<Func<T, TResult>> selector)
        => Wrap(_source.Select(selector));

    IReadOnlyQueryable<TResult> IReadOnlyQueryable<T>.SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector)
        => Wrap(_source.SelectMany(selector));

    IReadOnlyQueryable<TResult> IReadOnlyQueryable<T>.SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector)
        => Wrap(_source.SelectMany(collectionSelector, resultSelector));

    IReadOnlyQueryable<IGrouping<TKey, T>> IReadOnlyQueryable<T>.GroupBy<TKey>(
        Expression<Func<T, TKey>> keySelector)
        => Wrap(_source.GroupBy(keySelector));

    IReadOnlyQueryable<IGrouping<TKey, TElement>> IReadOnlyQueryable<T>.GroupBy<TKey, TElement>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector)
        => Wrap(_source.GroupBy(keySelector, elementSelector));

    IReadOnlyOrderedQueryable<T> IReadOnlyQueryable<T>.OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(_source.OrderBy(keySelector));

    IReadOnlyOrderedQueryable<T> IReadOnlyQueryable<T>.OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(_source.OrderByDescending(keySelector));

    IReadOnlyQueryable<T> IReadOnlyQueryable<T>.Skip(int count)
        => Wrap(_source.Skip(count));

    IReadOnlyQueryable<T> IReadOnlyQueryable<T>.Take(int count)
        => Wrap(_source.Take(count));

    IReadOnlyQueryable<T> IReadOnlyQueryable<T>.Distinct()
        => Wrap(_source.Distinct());


    // IReadOnlyOrderedQueryable<T> implementations
    IReadOnlyOrderedQueryable<T> IReadOnlyOrderedQueryable<T>.ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(((IOrderedQueryable<T>)_source).ThenBy(keySelector));

    IReadOnlyOrderedQueryable<T> IReadOnlyOrderedQueryable<T>.ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(((IOrderedQueryable<T>)_source).ThenByDescending(keySelector));

    IReadOnlyQueryable<T> IReadOnlyOrderedQueryable<T>.Where(Expression<Func<T, bool>> predicate)
        => Wrap(_source.Where(predicate));

    IReadOnlyQueryable<TResult> IReadOnlyOrderedQueryable<T>.Select<TResult>(Expression<Func<T, TResult>> selector)
        => Wrap(_source.Select(selector));

    IReadOnlyQueryable<TResult> IReadOnlyOrderedQueryable<T>.SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector)
        => Wrap(_source.SelectMany(selector));

    IReadOnlyQueryable<TResult> IReadOnlyOrderedQueryable<T>.SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector)
        => Wrap(_source.SelectMany(collectionSelector, resultSelector));

    IReadOnlyQueryable<IGrouping<TKey, T>> IReadOnlyOrderedQueryable<T>.GroupBy<TKey>(
        Expression<Func<T, TKey>> keySelector)
        => Wrap(_source.GroupBy(keySelector));

    IReadOnlyQueryable<IGrouping<TKey, TElement>> IReadOnlyOrderedQueryable<T>.GroupBy<TKey, TElement>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector)
        => Wrap(_source.GroupBy(keySelector, elementSelector));

    IReadOnlyQueryable<T> IReadOnlyOrderedQueryable<T>.Skip(int count)
        => Wrap(_source.Skip(count));

    IReadOnlyQueryable<T> IReadOnlyOrderedQueryable<T>.Take(int count)
        => Wrap(_source.Take(count));

    IReadOnlyQueryable<T> IReadOnlyOrderedQueryable<T>.Distinct()
        => Wrap(_source.Distinct());


}
