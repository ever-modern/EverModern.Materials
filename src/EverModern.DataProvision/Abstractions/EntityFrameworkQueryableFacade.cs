using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace EverModern.DataProvision.Abstractions;

/// <summary>
/// Entity Framework-aware facade over <see cref="IQueryable{T}"/>.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public class EntityFrameworkQueryableFacade<T>(IQueryable<T> source) : IEntityFrameworkQueryable<T>, IQueryableSourceAccessor<T>
{
    static readonly Lazy<MethodInfo?> NativeLeftJoinMethod = new(ResolveNativeLeftJoinMethod);

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

    sealed class LeftJoinCarrier<TOuter, TInner>
    {
        public required TOuter Outer { get; init; }
        public required IEnumerable<TInner> Inners { get; init; }
    }

    sealed class ReplaceExpressionVisitor(ParameterExpression source, Expression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == source ? target : base.VisitParameter(node);
    }

    /// <inheritdoc />
    public Expression Expression => _source.Expression;

    /// <inheritdoc />
    public IQueryable<T> Source => _source;

    EntityFrameworkQueryableFacade<TResult> Wrap<TResult>(IQueryable<TResult> query)
        => new(query);

    IQueryable<T> ApplySingleGenericExtension(MethodInfo method)
        => (IQueryable<T>)method.MakeGenericMethod(typeof(T)).Invoke(null, [_source])!;

    IQueryable<T> ApplyInclude<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath)
        => (IQueryable<T>)IncludeMethod.MakeGenericMethod(typeof(T), typeof(TProperty))
            .Invoke(null, [_source, navigationPropertyPath])!;

    static MethodInfo? ResolveNativeLeftJoinMethod()
    {
        var queryableLeftJoin = typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(IsNativeLeftJoinCandidate);
        if (queryableLeftJoin is not null)
        {
            return queryableLeftJoin;
        }

        var efLeftJoin = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(IsNativeLeftJoinCandidate);
        if (efLeftJoin is not null)
        {
            return efLeftJoin;
        }

        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    return ex.Types.OfType<Type>();
                }
            })
            .Where(t => t is { IsAbstract: true, IsSealed: true }
                && t.Name == "LinqExtensions"
                && t.Namespace is not null
                && t.Namespace.StartsWith("LinqToDB", StringComparison.Ordinal))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .FirstOrDefault(IsNativeLeftJoinCandidate);
    }

    static bool IsNativeLeftJoinCandidate(MethodInfo method)
    {
        if (method.Name != "LeftJoin" || !method.IsGenericMethodDefinition)
        {
            return false;
        }

        var genericArguments = method.GetGenericArguments();
        if (genericArguments.Length != 4)
        {
            return false;
        }

        var parameters = method.GetParameters();
        if (parameters.Length != 5)
        {
            return false;
        }

        return parameters[0].ParameterType.IsGenericType
            && parameters[1].ParameterType.IsGenericType
            && parameters[2].ParameterType.IsGenericType
            && parameters[3].ParameterType.IsGenericType
            && parameters[4].ParameterType.IsGenericType
            && parameters[2].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>)
            && parameters[3].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>)
            && parameters[4].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>);
    }

    static IQueryable<TInner> GetSource<TInner>(object inner, string paramName)
        => inner is IQueryableSourceAccessor<TInner> sourceAccessor
            ? sourceAccessor.Source
            : throw new ArgumentException(
                $"Parameter '{paramName}' must implement {nameof(IQueryableSourceAccessor<TInner>)}.",
                paramName);

    IQueryable<TResult> ApplyLeftJoin<TInner, TKey, TResult>(
        IQueryable<TInner> innerSource,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector)
    {
        if (NativeLeftJoinMethod.Value is MethodInfo nativeLeftJoin)
        {
            return (IQueryable<TResult>)nativeLeftJoin
                .MakeGenericMethod(typeof(T), typeof(TInner), typeof(TKey), typeof(TResult))
                .Invoke(null, [_source, innerSource, outerKeySelector, innerKeySelector, resultSelector])!;
        }

        return _source
            .GroupJoin(
                innerSource,
                outerKeySelector,
                innerKeySelector,
                (outerItem, innerItems) => new LeftJoinCarrier<T, TInner>
                {
                    Outer = outerItem,
                    Inners = innerItems,
                })
            .SelectMany(
                x => x.Inners.DefaultIfEmpty(),
                CreateLeftJoinResultSelector(resultSelector));
    }

    static Expression<Func<LeftJoinCarrier<T, TInner>, TInner, TResult>> CreateLeftJoinResultSelector<TInner, TResult>(
        Expression<Func<T, TInner, TResult>> resultSelector)
    {
        var carrier = Expression.Parameter(typeof(LeftJoinCarrier<T, TInner>), "x");
        var inner = Expression.Parameter(typeof(TInner), "innerItem");

        var withOuter = new ReplaceExpressionVisitor(
            resultSelector.Parameters[0],
            Expression.Property(carrier, nameof(LeftJoinCarrier<T, TInner>.Outer)))
            .Visit(resultSelector.Body)!;

        var body = new ReplaceExpressionVisitor(resultSelector.Parameters[1], inner)
            .Visit(withOuter)!;

        return Expression.Lambda<Func<LeftJoinCarrier<T, TInner>, TInner, TResult>>(body, carrier, inner);
    }

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

    IReadOnlyQueryable<TResult> IReadOnlyQueryable<T>.GroupJoin<TInner, TKey, TResult>(
        IReadOnlyQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector)
        => Wrap(_source.GroupJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

    IReadOnlyQueryable<TResult> IReadOnlyQueryable<T>.LeftJoin<TInner, TKey, TResult>(
        IReadOnlyQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector)
        => Wrap(ApplyLeftJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

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

    IReadOnlyQueryable<TResult> IReadOnlyOrderedQueryable<T>.GroupJoin<TInner, TKey, TResult>(
        IReadOnlyOrderedQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector)
        => Wrap(_source.GroupJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

    IReadOnlyQueryable<TResult> IReadOnlyOrderedQueryable<T>.LeftJoin<TInner, TKey, TResult>(
        IReadOnlyOrderedQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector)
        => Wrap(ApplyLeftJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

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

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.GroupJoin<TInner, TKey, TResult>(
        IReadOnlyAsyncQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector)
        => Wrap(_source.GroupJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.LeftJoin<TInner, TKey, TResult>(
        IReadOnlyAsyncQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector)
        => Wrap(ApplyLeftJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

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

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.GroupJoin<TInner, TKey, TResult>(
        IReadOnlyAsyncOrderedQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector)
        => Wrap(_source.GroupJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.LeftJoin<TInner, TKey, TResult>(
        IReadOnlyAsyncOrderedQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector)
        => Wrap(ApplyLeftJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

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

    IAsyncOnlyQueryable<TResult> IAsyncOnlyQueryable<T>.GroupJoin<TInner, TKey, TResult>(
        IAsyncOnlyQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector)
        => Wrap(_source.GroupJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

    IAsyncOnlyQueryable<TResult> IAsyncOnlyQueryable<T>.LeftJoin<TInner, TKey, TResult>(
        IAsyncOnlyQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector)
        => Wrap(ApplyLeftJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

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

    IAsyncOnlyQueryable<TResult> IAsyncOnlyOrderedQueryable<T>.GroupJoin<TInner, TKey, TResult>(
        IAsyncOnlyOrderedQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector)
        => Wrap(_source.GroupJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

    IAsyncOnlyQueryable<TResult> IAsyncOnlyOrderedQueryable<T>.LeftJoin<TInner, TKey, TResult>(
        IAsyncOnlyOrderedQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector)
        => Wrap(ApplyLeftJoin(GetSource<TInner>(inner, nameof(inner)), outerKeySelector, innerKeySelector, resultSelector));

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
