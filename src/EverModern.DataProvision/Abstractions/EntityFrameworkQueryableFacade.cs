using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace EverModern.DataProvision.Abstractions;

/// <summary>
/// Entity Framework-aware facade over <see cref="IQueryable{T}"/>.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public class EntityFrameworkQueryableFacade<T>(IQueryable<T> source) : IEntityFrameworkQueryable<T>
{
    static readonly MethodInfo IncludeMethod = typeof(EntityFrameworkQueryableExtensions)
        .GetMethods()
        .Single(m => m.Name == nameof(EntityFrameworkQueryableExtensions.Include)
            && m.GetParameters().Length == 2);
    static readonly MethodInfo AsNoTrackingMethod = typeof(EntityFrameworkQueryableExtensions)
        .GetMethods()
        .Single(m => m.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking)
            && m.GetParameters().Length == 1);
    static readonly MethodInfo AsTrackingMethod = typeof(EntityFrameworkQueryableExtensions)
        .GetMethods()
        .Single(m => m.Name == nameof(EntityFrameworkQueryableExtensions.AsTracking)
            && m.GetParameters().Length == 1);
    static readonly MethodInfo AsSplitQueryMethod = typeof(RelationalQueryableExtensions)
        .GetMethods()
        .Single(m => m.Name == nameof(RelationalQueryableExtensions.AsSplitQuery)
            && m.GetParameters().Length == 1);
    static readonly MethodInfo AsSingleQueryMethod = typeof(RelationalQueryableExtensions)
        .GetMethods()
        .Single(m => m.Name == nameof(RelationalQueryableExtensions.AsSingleQuery)
            && m.GetParameters().Length == 1);

    readonly IQueryable<T> _source = source;

    /// <inheritdoc />
    public Expression Expression => _source.Expression;

    EntityFrameworkQueryableFacade<TResult> Wrap<TResult>(IQueryable<TResult> query)
        => new(query);

    IQueryable<T> ApplySingleGenericExtension(MethodInfo method)
        => (IQueryable<T>)method.MakeGenericMethod(typeof(T)).Invoke(null, [_source])!;

    IQueryable<T> ApplyInclude<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath)
        => (IQueryable<T>)IncludeMethod.MakeGenericMethod(typeof(T), typeof(TProperty))
            .Invoke(null, [_source, navigationPropertyPath])!;

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

    Task<List<T>> IAsyncMaterializable<T>.ToListAsync(CancellationToken cancellationToken)
        => _source.ToListAsync(cancellationToken);

    Task<T[]> IAsyncMaterializable<T>.ToArrayAsync(CancellationToken cancellationToken)
        => _source.ToArrayAsync(cancellationToken);

    Task<Dictionary<TKey, T>> IAsyncMaterializable<T>.ToDictionaryAsync<TKey>(
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken)
        => _source.ToDictionaryAsync(keySelector, cancellationToken);

    Task<Dictionary<TKey, TElement>> IAsyncMaterializable<T>.ToDictionaryAsync<TKey, TElement>(
        Func<T, TKey> keySelector,
        Func<T, TElement> elementSelector,
        CancellationToken cancellationToken)
        => _source.ToDictionaryAsync(keySelector, elementSelector, cancellationToken);

    Task<T> IAsyncMaterializable<T>.FirstAsync(CancellationToken cancellationToken)
        => _source.FirstAsync(cancellationToken);

    Task<T> IAsyncMaterializable<T>.FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        => _source.FirstAsync(predicate, cancellationToken);

    Task<T?> IAsyncMaterializable<T>.FirstOrDefaultAsync(CancellationToken cancellationToken)
        => _source.FirstOrDefaultAsync(cancellationToken);

    Task<T?> IAsyncMaterializable<T>.FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        => _source.FirstOrDefaultAsync(predicate, cancellationToken);

    Task<T> IAsyncMaterializable<T>.SingleAsync(CancellationToken cancellationToken)
        => _source.SingleAsync(cancellationToken);

    Task<T> IAsyncMaterializable<T>.SingleAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        => _source.SingleAsync(predicate, cancellationToken);

    Task<T?> IAsyncMaterializable<T>.SingleOrDefaultAsync(CancellationToken cancellationToken)
        => _source.SingleOrDefaultAsync(cancellationToken);

    Task<T?> IAsyncMaterializable<T>.SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        => _source.SingleOrDefaultAsync(predicate, cancellationToken);

    Task<bool> IAsyncMaterializable<T>.AnyAsync(CancellationToken cancellationToken)
        => _source.AnyAsync(cancellationToken);

    Task<bool> IAsyncMaterializable<T>.AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        => _source.AnyAsync(predicate, cancellationToken);

    Task<int> IAsyncMaterializable<T>.CountAsync(CancellationToken cancellationToken)
        => _source.CountAsync(cancellationToken);

    Task<int> IAsyncMaterializable<T>.CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        => _source.CountAsync(predicate, cancellationToken);

    Task<long> IAsyncMaterializable<T>.LongCountAsync(CancellationToken cancellationToken)
        => _source.LongCountAsync(cancellationToken);

    Task<long> IAsyncMaterializable<T>.LongCountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        => _source.LongCountAsync(predicate, cancellationToken);

    Task<decimal> IAsyncMaterializable<T>.SumAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken)
        => _source.SumAsync(selector, cancellationToken);

    Task<decimal> IAsyncMaterializable<T>.AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken)
        => _source.AverageAsync(selector, cancellationToken);

    Task<TResult> IAsyncMaterializable<T>.MinAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken)
        => _source.MinAsync(selector, cancellationToken);

    Task<TResult> IAsyncMaterializable<T>.MaxAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken)
        => _source.MaxAsync(selector, cancellationToken);

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

    IReadOnlyQueryable<IGrouping<TKey, T>> IReadOnlyQueryable<T>.GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
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

    IReadOnlyQueryable<IGrouping<TKey, T>> IReadOnlyOrderedQueryable<T>.GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
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

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncQueryable<T>.Where(Expression<Func<T, bool>> predicate)
        => Wrap(_source.Where(predicate));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.Select<TResult>(Expression<Func<T, TResult>> selector)
        => Wrap(_source.Select(selector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector)
        => Wrap(_source.SelectMany(selector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector)
        => Wrap(_source.SelectMany(collectionSelector, resultSelector));

    IReadOnlyAsyncQueryable<IGrouping<TKey, T>> IReadOnlyAsyncQueryable<T>.GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(_source.GroupBy(keySelector));

    IReadOnlyAsyncQueryable<IGrouping<TKey, TElement>> IReadOnlyAsyncQueryable<T>.GroupBy<TKey, TElement>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector)
        => Wrap(_source.GroupBy(keySelector, elementSelector));

    IReadOnlyAsyncOrderedQueryable<T> IReadOnlyAsyncQueryable<T>.OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(_source.OrderBy(keySelector));

    IReadOnlyAsyncOrderedQueryable<T> IReadOnlyAsyncQueryable<T>.OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(_source.OrderByDescending(keySelector));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncQueryable<T>.Skip(int count)
        => Wrap(_source.Skip(count));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncQueryable<T>.Take(int count)
        => Wrap(_source.Take(count));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncQueryable<T>.Distinct()
        => Wrap(_source.Distinct());

    IReadOnlyAsyncOrderedQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(((IOrderedQueryable<T>)_source).ThenBy(keySelector));

    IReadOnlyAsyncOrderedQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(((IOrderedQueryable<T>)_source).ThenByDescending(keySelector));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.Where(Expression<Func<T, bool>> predicate)
        => Wrap(_source.Where(predicate));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.Select<TResult>(Expression<Func<T, TResult>> selector)
        => Wrap(_source.Select(selector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector)
        => Wrap(_source.SelectMany(selector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector)
        => Wrap(_source.SelectMany(collectionSelector, resultSelector));

    IReadOnlyAsyncQueryable<IGrouping<TKey, T>> IReadOnlyAsyncOrderedQueryable<T>.GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(_source.GroupBy(keySelector));

    IReadOnlyAsyncQueryable<IGrouping<TKey, TElement>> IReadOnlyAsyncOrderedQueryable<T>.GroupBy<TKey, TElement>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector)
        => Wrap(_source.GroupBy(keySelector, elementSelector));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.Skip(int count)
        => Wrap(_source.Skip(count));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.Take(int count)
        => Wrap(_source.Take(count));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.Distinct()
        => Wrap(_source.Distinct());

    IAsyncOnlyQueryable<T> IAsyncOnlyQueryable<T>.Where(Expression<Func<T, bool>> predicate)
        => Wrap(_source.Where(predicate));

    IAsyncOnlyQueryable<TResult> IAsyncOnlyQueryable<T>.Select<TResult>(Expression<Func<T, TResult>> selector)
        => Wrap(_source.Select(selector));

    IAsyncOnlyQueryable<TResult> IAsyncOnlyQueryable<T>.SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector)
        => Wrap(_source.SelectMany(selector));

    IAsyncOnlyQueryable<TResult> IAsyncOnlyQueryable<T>.SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector)
        => Wrap(_source.SelectMany(collectionSelector, resultSelector));

    IAsyncOnlyQueryable<IGrouping<TKey, T>> IAsyncOnlyQueryable<T>.GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(_source.GroupBy(keySelector));

    IAsyncOnlyQueryable<IGrouping<TKey, TElement>> IAsyncOnlyQueryable<T>.GroupBy<TKey, TElement>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector)
        => Wrap(_source.GroupBy(keySelector, elementSelector));

    IAsyncOnlyOrderedQueryable<T> IAsyncOnlyQueryable<T>.OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(_source.OrderBy(keySelector));

    IAsyncOnlyOrderedQueryable<T> IAsyncOnlyQueryable<T>.OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(_source.OrderByDescending(keySelector));

    IAsyncOnlyQueryable<T> IAsyncOnlyQueryable<T>.Skip(int count)
        => Wrap(_source.Skip(count));

    IAsyncOnlyQueryable<T> IAsyncOnlyQueryable<T>.Take(int count)
        => Wrap(_source.Take(count));

    IAsyncOnlyQueryable<T> IAsyncOnlyQueryable<T>.Distinct()
        => Wrap(_source.Distinct());

    IAsyncOnlyOrderedQueryable<T> IAsyncOnlyOrderedQueryable<T>.ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(((IOrderedQueryable<T>)_source).ThenBy(keySelector));

    IAsyncOnlyOrderedQueryable<T> IAsyncOnlyOrderedQueryable<T>.ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(((IOrderedQueryable<T>)_source).ThenByDescending(keySelector));

    IAsyncOnlyQueryable<T> IAsyncOnlyOrderedQueryable<T>.Where(Expression<Func<T, bool>> predicate)
        => Wrap(_source.Where(predicate));

    IAsyncOnlyQueryable<TResult> IAsyncOnlyOrderedQueryable<T>.Select<TResult>(Expression<Func<T, TResult>> selector)
        => Wrap(_source.Select(selector));

    IAsyncOnlyQueryable<TResult> IAsyncOnlyOrderedQueryable<T>.SelectMany<TResult>(Expression<Func<T, IEnumerable<TResult>>> selector)
        => Wrap(_source.SelectMany(selector));

    IAsyncOnlyQueryable<TResult> IAsyncOnlyOrderedQueryable<T>.SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector)
        => Wrap(_source.SelectMany(collectionSelector, resultSelector));

    IAsyncOnlyQueryable<IGrouping<TKey, T>> IAsyncOnlyOrderedQueryable<T>.GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => Wrap(_source.GroupBy(keySelector));

    IAsyncOnlyQueryable<IGrouping<TKey, TElement>> IAsyncOnlyOrderedQueryable<T>.GroupBy<TKey, TElement>(
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector)
        => Wrap(_source.GroupBy(keySelector, elementSelector));

    IAsyncOnlyQueryable<T> IAsyncOnlyOrderedQueryable<T>.Skip(int count)
        => Wrap(_source.Skip(count));

    IAsyncOnlyQueryable<T> IAsyncOnlyOrderedQueryable<T>.Take(int count)
        => Wrap(_source.Take(count));

    IAsyncOnlyQueryable<T> IAsyncOnlyOrderedQueryable<T>.Distinct()
        => Wrap(_source.Distinct());

    IEntityFrameworkQueryable<T> IEntityFrameworkQueryable<T>.Include<TProperty>(
        Expression<Func<T, TProperty>> navigationPropertyPath)
        => Wrap(ApplyInclude(navigationPropertyPath));

    IEntityFrameworkQueryable<T> IEntityFrameworkQueryable<T>.AsNoTracking()
        => Wrap(ApplySingleGenericExtension(AsNoTrackingMethod));

    IEntityFrameworkQueryable<T> IEntityFrameworkQueryable<T>.AsTracking()
        => Wrap(ApplySingleGenericExtension(AsTrackingMethod));

    IEntityFrameworkQueryable<T> IEntityFrameworkQueryable<T>.AsSplitQuery()
        => Wrap(ApplySingleGenericExtension(AsSplitQueryMethod));

    IEntityFrameworkQueryable<T> IEntityFrameworkQueryable<T>.AsSingleQuery()
        => Wrap(ApplySingleGenericExtension(AsSingleQueryMethod));

    IEntityFrameworkQueryable<T> IEntityFrameworkQueryable<T>.TagWith(string tag)
        => Wrap(_source.TagWith(tag));

    Task<int> IEntityFrameworkQueryable<T>.ExecuteUpdateAsync(
        Action<UpdateSettersBuilder<T>> setPropertyCalls,
        CancellationToken cancellationToken)
        => _source.ExecuteUpdateAsync(setPropertyCalls, cancellationToken);

    Task<int> IEntityFrameworkQueryable<T>.ExecuteDeleteAsync(CancellationToken cancellationToken)
        => _source.ExecuteDeleteAsync(cancellationToken);
}
