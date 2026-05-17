using System.Linq.Expressions;
using EverModern.DataProvision.Abstractions;
using LinqToDB.Async;

namespace EverModern.QueryKit;

public class AsyncQueryableFacade<T>(IQueryable<T> source)
    : QueryableLimittingFacade<T>(source),
        IAsyncEnumerable<T>,
        IOrderedQueryable<T>,
        IReadOnlyAsyncQueryable<T>,
        IReadOnlyAsyncOrderedQueryable<T>
{
    AsyncQueryableFacade<TResult> WrapAsync<TResult>(IQueryable<TResult> query) => new(query);

    // IOrderedQueryable<T> / IQueryable<T>
    public Type ElementType => Source.ElementType;
    public IQueryProvider Provider => Source.Provider;

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => throw new NotSupportedException("Synchronous enumeration is not supported. Use async enumeration instead.");

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        => throw new NotSupportedException("Synchronous enumeration is not supported. Use async enumeration instead.");

    // IAsyncEnumerable<T>
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => ((IAsyncEnumerable<T>)Source).GetAsyncEnumerator(cancellationToken);

    // IAsyncMaterializable<T>
    Task<List<T>> IAsyncMaterializable<T>.ToListAsync(CancellationToken cancellationToken)
        => Source.ToListAsync(cancellationToken);

    Task<T[]> IAsyncMaterializable<T>.ToArrayAsync(CancellationToken cancellationToken)
        => Source.ToArrayAsync(cancellationToken);

    Task<Dictionary<TKey, T>> IAsyncMaterializable<T>.ToDictionaryAsync<TKey>(
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken)
        => Source.ToDictionaryAsync(keySelector, cancellationToken);

    Task<Dictionary<TKey, TElement>> IAsyncMaterializable<T>.ToDictionaryAsync<TKey, TElement>(
        Func<T, TKey> keySelector,
        Func<T, TElement> elementSelector,
        CancellationToken cancellationToken)
        => Source.ToDictionaryAsync(keySelector, elementSelector, cancellationToken);

    Task<T> IAsyncMaterializable<T>.FirstAsync(CancellationToken cancellationToken)
        => Source.FirstAsync(cancellationToken);

    Task<T> IAsyncMaterializable<T>.FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        => Source.FirstAsync(predicate, cancellationToken);

    Task<T?> IAsyncMaterializable<T>.FirstOrDefaultAsync(CancellationToken cancellationToken)
        => Source.FirstOrDefaultAsync(cancellationToken);

    Task<T?> IAsyncMaterializable<T>.FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        => Source.FirstOrDefaultAsync(predicate, cancellationToken);

    Task<T> IAsyncMaterializable<T>.SingleAsync(CancellationToken cancellationToken)
        => Source.SingleAsync(cancellationToken);

    Task<T> IAsyncMaterializable<T>.SingleAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        => Source.SingleAsync(predicate, cancellationToken);

    Task<T?> IAsyncMaterializable<T>.SingleOrDefaultAsync(CancellationToken cancellationToken)
        => Source.SingleOrDefaultAsync(cancellationToken);

    Task<T?> IAsyncMaterializable<T>.SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        => Source.SingleOrDefaultAsync(predicate, cancellationToken);

    Task<bool> IAsyncMaterializable<T>.AnyAsync(CancellationToken cancellationToken)
        => Source.AnyAsync(cancellationToken);

    Task<bool> IAsyncMaterializable<T>.AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        => Source.AnyAsync(predicate, cancellationToken);

    Task<int> IAsyncMaterializable<T>.CountAsync(CancellationToken cancellationToken)
        => Source.CountAsync(cancellationToken);

    Task<int> IAsyncMaterializable<T>.CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        => Source.CountAsync(predicate, cancellationToken);

    Task<long> IAsyncMaterializable<T>.LongCountAsync(CancellationToken cancellationToken)
        => Source.LongCountAsync(cancellationToken);

    Task<long> IAsyncMaterializable<T>.LongCountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        => Source.LongCountAsync(predicate, cancellationToken);

    Task<decimal> IAsyncMaterializable<T>.SumAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken)
        => Source.SumAsync(selector, cancellationToken);

    Task<decimal> IAsyncMaterializable<T>.AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken)
        => Source.AverageAsync(selector, cancellationToken);

    Task<TResult> IAsyncMaterializable<T>.MinAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken)
        => Source.MinAsync(selector, cancellationToken);

    Task<TResult> IAsyncMaterializable<T>.MaxAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken)
        => Source.MaxAsync(selector, cancellationToken);

    // IReadOnlyAsyncQueryable<T>
    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncQueryable<T>.Where(Expression<Func<T, bool>> predicate)
        => WrapAsync(Source.Where(predicate));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.Select<TResult>(Expression<Func<T, TResult>> selector)
        => WrapAsync(Source.Select(selector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector)
        => WrapAsync(Source.SelectMany(selector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector)
        => WrapAsync(Source.SelectMany(collectionSelector, resultSelector));

    IReadOnlyAsyncQueryable<IGrouping<TKey, T>> IReadOnlyAsyncQueryable<T>.GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => WrapAsync(Source.GroupBy(keySelector));

    IReadOnlyAsyncQueryable<IGrouping<TKey, TElement>> IReadOnlyAsyncQueryable<T>.GroupBy<TKey, TElement>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector)
        => WrapAsync(Source.GroupBy(keySelector, elementSelector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.GroupJoin<TInner, TKey, TResult>(
        IReadOnlyAsyncQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector)
        => WrapAsync(Source.GroupJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.LeftJoin<TInner, TKey, TResult>(
        IReadOnlyAsyncQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector)
        => WrapAsync(ApplyLeftJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

    IReadOnlyAsyncOrderedQueryable<T> IReadOnlyAsyncQueryable<T>.OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => WrapAsync(Source.OrderBy(keySelector));

    IReadOnlyAsyncOrderedQueryable<T> IReadOnlyAsyncQueryable<T>.OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        => WrapAsync(Source.OrderByDescending(keySelector));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncQueryable<T>.Skip(int count)
        => WrapAsync(Source.Skip(count));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncQueryable<T>.Take(int count)
        => WrapAsync(Source.Take(count));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncQueryable<T>.Distinct()
        => WrapAsync(Source.Distinct());

    // IReadOnlyAsyncOrderedQueryable<T>
    IReadOnlyAsyncOrderedQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => WrapAsync(((IOrderedQueryable<T>)Source).ThenBy(keySelector));

    IReadOnlyAsyncOrderedQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        => WrapAsync(((IOrderedQueryable<T>)Source).ThenByDescending(keySelector));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.Where(Expression<Func<T, bool>> predicate)
        => WrapAsync(Source.Where(predicate));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.Select<TResult>(Expression<Func<T, TResult>> selector)
        => WrapAsync(Source.Select(selector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector)
        => WrapAsync(Source.SelectMany(selector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector)
        => WrapAsync(Source.SelectMany(collectionSelector, resultSelector));

    IReadOnlyAsyncQueryable<IGrouping<TKey, T>> IReadOnlyAsyncOrderedQueryable<T>.GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => WrapAsync(Source.GroupBy(keySelector));

    IReadOnlyAsyncQueryable<IGrouping<TKey, TElement>> IReadOnlyAsyncOrderedQueryable<T>.GroupBy<TKey, TElement>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector)
        => WrapAsync(Source.GroupBy(keySelector, elementSelector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.GroupJoin<TInner, TKey, TResult>(
        IReadOnlyAsyncOrderedQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector)
        => WrapAsync(Source.GroupJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.LeftJoin<TInner, TKey, TResult>(
        IReadOnlyAsyncOrderedQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector)
        => WrapAsync(ApplyLeftJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.Skip(int count)
        => WrapAsync(Source.Skip(count));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.Take(int count)
        => WrapAsync(Source.Take(count));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.Distinct()
        => WrapAsync(Source.Distinct());
}
