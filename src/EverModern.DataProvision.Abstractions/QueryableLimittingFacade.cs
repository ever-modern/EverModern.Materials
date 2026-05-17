using System.Linq.Expressions;
using System.Reflection;

namespace EverModern.DataProvision.Abstractions;

/// <summary>
/// Wraps an <see cref="IQueryable{T}"/> and exposes limited LINQ-to-entities surfaces via
/// explicit interface implementations.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public class QueryableLimittingFacade<T>(IQueryable<T> source) : 
    IReadOnlyQueryable<T>, IReadOnlyOrderedQueryable<T>, IQueryableSourceAccessor<T>
{
    static readonly Lazy<MethodInfo?> NativeLeftJoinMethod = new(ResolveNativeLeftJoinMethod);

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

    /// <inheritdoc/>
    public Expression Expression => _source.Expression;

    /// <inheritdoc />
    public IQueryable<T> Source => _source;

    QueryableLimittingFacade<TResult> Wrap<TResult>(IQueryable<TResult> query)
        => new(query);

    static MethodInfo? ResolveNativeLeftJoinMethod()
        => AppDomain.CurrentDomain
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

    protected static IQueryable<TInner> GetSource<TInner>(object inner, string paramName)
        => inner is IQueryableSourceAccessor<TInner> sourceAccessor
            ? sourceAccessor.Source
            : throw new ArgumentException(
                $"Parameter '{paramName}' must implement {nameof(IQueryableSourceAccessor<TInner>)}.",
                paramName);

    protected IQueryable<TResult> ApplyLeftJoin<TInner, TKey, TResult>(
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


}
