using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using EverModern.DataProvision.Abstractions;
using LinqToDB;
using LinqToDB.Async;

namespace EverModern.QueryKit;

public class DeferredQueryable<T>(
    IQueryable<T> currentQuery,
    Func<IDataContext, IQueryable<T>> rootQueryFactory,
    ConnectionFactory<IDataContext> dataConnectionFactory
)
    : IQueryable<T>,
        IAsyncEnumerable<T>,
        IOrderedQueryable<T>,
        IReadOnlyAsyncQueryable<T>,
        IReadOnlyAsyncOrderedQueryable<T>,
        IQueryableSourceAccessor<T>
{
    public DeferredQueryable(
        ConnectionFactory<IDataContext> dataConnectionFactory,
        IDataContext builderConnection
    )
        : this(
            currentQuery: CreateTableQueryable(builderConnection),
            rootQueryFactory: CreateTableQueryable,
            dataConnectionFactory: dataConnectionFactory
        ) { }

    public Type ElementType => typeof(T);

    public Expression Expression { get; } = currentQuery.Expression;

    public IQueryProvider Provider { get; } =
        new DeferredQueryProvider(
            source: currentQuery.Provider,
            rootQueryFactory: dataContext => rootQueryFactory(dataContext),
            dataConnectionFactory: dataConnectionFactory
        );

    /// <inheritdoc />
    public IQueryable<T> Source => this;

    static readonly MethodInfo GetTableMethod = typeof(DataExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m =>
            m.Name == nameof(DataExtensions.GetTable)
            && m.IsGenericMethodDefinition
            && m.GetParameters() is [{ ParameterType: var firstParameterType }]
            && firstParameterType == typeof(IDataContext)
        );

    static readonly Lazy<MethodInfo?> NativeLeftJoinMethod = new(ResolveNativeLeftJoinMethod);

    sealed class LeftJoinCarrier<TOuter, TInner>
    {
        public required TOuter Outer { get; init; }
        public required IEnumerable<TInner> Inners { get; init; }
    }

    sealed class ReplaceExpressionVisitor(ParameterExpression source, Expression target)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }

    static IQueryable<T> CreateTableQueryable(IDataContext dataContext)
    {
        if (!typeof(T).IsClass)
        {
            throw new NotSupportedException(
                $"The root table query type {typeof(T)} must be a reference type."
            );
        }

        return (IQueryable<T>)(
            GetTableMethod.MakeGenericMethod(typeof(T)).Invoke(null, [dataContext])
            ?? throw new InvalidOperationException("Could not construct table query.")
        );
    }

    static MethodInfo? ResolveNativeLeftJoinMethod() =>
        AppDomain
            .CurrentDomain.GetAssemblies()
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
            .Where(t =>
                t is { IsAbstract: true, IsSealed: true }
                && t.Name == "LinqExtensions"
                && t.Namespace is not null
                && t.Namespace.StartsWith("LinqToDB", StringComparison.Ordinal)
            )
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

    static IQueryable<TInner> GetSource<TInner>(object inner, string paramName) =>
        inner is IQueryableSourceAccessor<TInner> accessor
            ? accessor.Source
            : throw new ArgumentException(
                $"Parameter '{paramName}' must implement {nameof(IQueryableSourceAccessor<TInner>)}.",
                paramName
            );

    IQueryable<TResult> ApplyLeftJoin<TInner, TKey, TResult>(
        IQueryable<TInner> innerSource,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector
    )
    {
        if (NativeLeftJoinMethod.Value is MethodInfo nativeLeftJoin)
        {
            return (IQueryable<TResult>)
                nativeLeftJoin
                    .MakeGenericMethod(typeof(T), typeof(TInner), typeof(TKey), typeof(TResult))
                    .Invoke(
                        null,
                        [this, innerSource, outerKeySelector, innerKeySelector, resultSelector]
                    )!;
        }

        return ((IQueryable<T>)this)
            .GroupJoin(
                innerSource,
                outerKeySelector,
                innerKeySelector,
                (outerItem, innerItems) =>
                    new LeftJoinCarrier<T, TInner> { Outer = outerItem, Inners = innerItems }
            )
            .SelectMany(
                x => x.Inners.DefaultIfEmpty(),
                CreateLeftJoinResultSelector(resultSelector)
            );
    }

    static Expression<
        Func<LeftJoinCarrier<T, TInner>, TInner, TResult>
    > CreateLeftJoinResultSelector<TInner, TResult>(
        Expression<Func<T, TInner, TResult>> resultSelector
    )
    {
        var carrier = Expression.Parameter(typeof(LeftJoinCarrier<T, TInner>), "x");
        var inner = Expression.Parameter(typeof(TInner), "innerItem");

        var withOuter = new ReplaceExpressionVisitor(
            resultSelector.Parameters[0],
            Expression.Property(carrier, nameof(LeftJoinCarrier<T, TInner>.Outer))
        ).Visit(resultSelector.Body)!;

        var body = new ReplaceExpressionVisitor(resultSelector.Parameters[1], inner).Visit(
            withOuter
        )!;

        return Expression.Lambda<Func<LeftJoinCarrier<T, TInner>, TInner, TResult>>(
            body,
            carrier,
            inner
        );
    }

    // Recreates the query against a fresh data connection, following the same pattern as
    // GetAsyncEnumerator, then executes the given delegate against the live provider query.
    async Task<TResult> ExecuteAsync<TResult>(
        Func<IQueryable<T>, Task<TResult>> execute,
        CancellationToken cancellationToken
    )
    {
        await using var dataConnection = await dataConnectionFactory(cancellationToken);
        var rootQuery = rootQueryFactory(dataConnection);
        var replacedQuery = rootQuery.Provider.CreateQuery<T>(currentQuery.Expression);
        return await execute(replacedQuery);
    }

    // Produces a new DeferredQueryable<TResult> by forwarding the shaped expression through the
    // DeferredQueryProvider, which mirrors the recreation pattern used in GetAsyncEnumerator.
    DeferredQueryable<TResult> Wrap<TResult>(IQueryable<TResult> shaped) =>
        (DeferredQueryable<TResult>)shaped;

    public async IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = default
    )
    {
        using var dataConnection = await dataConnectionFactory(cancellationToken);

        var rootQuery = rootQueryFactory(dataConnection);
        var replacedQuery = rootQuery.Provider.CreateQuery<T>(currentQuery.Expression);

        if (replacedQuery is not IAsyncEnumerable<T> asyncEnumerable)
        {
            throw new NotSupportedException("The query source does not support async enumeration.");
        }

        await foreach (var item in asyncEnumerable.WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        throw new NotSupportedException("Only async calls are supported on this IQueryable.");
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    Task<List<T>> IAsyncMaterializable<T>.ToListAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(q => q.ToListAsync(cancellationToken), cancellationToken);

    Task<T[]> IAsyncMaterializable<T>.ToArrayAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(q => q.ToArrayAsync(cancellationToken), cancellationToken);

    Task<Dictionary<TKey, T>> IAsyncMaterializable<T>.ToDictionaryAsync<TKey>(
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken
    ) => ExecuteAsync(q => q.ToDictionaryAsync(keySelector, cancellationToken), cancellationToken);

    Task<Dictionary<TKey, TElement>> IAsyncMaterializable<T>.ToDictionaryAsync<TKey, TElement>(
        Func<T, TKey> keySelector,
        Func<T, TElement> elementSelector,
        CancellationToken cancellationToken
    ) =>
        ExecuteAsync(
            q => q.ToDictionaryAsync(keySelector, elementSelector, cancellationToken),
            cancellationToken
        );

    Task<T> IAsyncMaterializable<T>.FirstAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(q => q.FirstAsync(cancellationToken), cancellationToken);

    Task<T> IAsyncMaterializable<T>.FirstAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken
    ) => ExecuteAsync(q => q.FirstAsync(predicate, cancellationToken), cancellationToken);

    Task<T?> IAsyncMaterializable<T>.FirstOrDefaultAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(q => q.FirstOrDefaultAsync(cancellationToken), cancellationToken);

    Task<T?> IAsyncMaterializable<T>.FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken
    ) => ExecuteAsync(q => q.FirstOrDefaultAsync(predicate, cancellationToken), cancellationToken);

    Task<T> IAsyncMaterializable<T>.SingleAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(q => q.SingleAsync(cancellationToken), cancellationToken);

    Task<T> IAsyncMaterializable<T>.SingleAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken
    ) => ExecuteAsync(q => q.SingleAsync(predicate, cancellationToken), cancellationToken);

    Task<T?> IAsyncMaterializable<T>.SingleOrDefaultAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(q => q.SingleOrDefaultAsync(cancellationToken), cancellationToken);

    Task<T?> IAsyncMaterializable<T>.SingleOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken
    ) => ExecuteAsync(q => q.SingleOrDefaultAsync(predicate, cancellationToken), cancellationToken);

    Task<bool> IAsyncMaterializable<T>.AnyAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(q => q.AnyAsync(cancellationToken), cancellationToken);

    Task<bool> IAsyncMaterializable<T>.AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken
    ) => ExecuteAsync(q => q.AnyAsync(predicate, cancellationToken), cancellationToken);

    Task<int> IAsyncMaterializable<T>.CountAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(q => q.CountAsync(cancellationToken), cancellationToken);

    Task<int> IAsyncMaterializable<T>.CountAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken
    ) => ExecuteAsync(q => q.CountAsync(predicate, cancellationToken), cancellationToken);

    Task<long> IAsyncMaterializable<T>.LongCountAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(q => q.LongCountAsync(cancellationToken), cancellationToken);

    Task<long> IAsyncMaterializable<T>.LongCountAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken
    ) => ExecuteAsync(q => q.LongCountAsync(predicate, cancellationToken), cancellationToken);

    Task<decimal> IAsyncMaterializable<T>.SumAsync(
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken
    ) => ExecuteAsync(q => q.SumAsync(selector, cancellationToken), cancellationToken);

    Task<decimal> IAsyncMaterializable<T>.AverageAsync(
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken
    ) => ExecuteAsync(q => q.AverageAsync(selector, cancellationToken), cancellationToken);

    Task<TResult> IAsyncMaterializable<T>.MinAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken
    ) => ExecuteAsync(q => q.MinAsync(selector, cancellationToken), cancellationToken);

    Task<TResult> IAsyncMaterializable<T>.MaxAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken
    ) => ExecuteAsync(q => q.MaxAsync(selector, cancellationToken), cancellationToken);

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncQueryable<T>.Where(
        Expression<Func<T, bool>> predicate
    ) => Wrap(((IQueryable<T>)this).Where(predicate));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.Select<TResult>(
        Expression<Func<T, TResult>> selector
    ) => Wrap(((IQueryable<T>)this).Select(selector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.SelectMany<TResult>(
        Expression<Func<T, IEnumerable<TResult>>> selector
    ) => Wrap(((IQueryable<T>)this).SelectMany(selector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.SelectMany<TCollection, TResult>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector
    ) => Wrap(((IQueryable<T>)this).SelectMany(collectionSelector, resultSelector));

    IReadOnlyAsyncQueryable<IGrouping<TKey, T>> IReadOnlyAsyncQueryable<T>.GroupBy<TKey>(
        Expression<Func<T, TKey>> keySelector
    ) => Wrap(((IQueryable<T>)this).GroupBy(keySelector));

    IReadOnlyAsyncQueryable<IGrouping<TKey, TElement>> IReadOnlyAsyncQueryable<T>.GroupBy<
        TKey,
        TElement
    >(Expression<Func<T, TKey>> keySelector, Expression<Func<T, TElement>> elementSelector) =>
        Wrap(((IQueryable<T>)this).GroupBy(keySelector, elementSelector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.GroupJoin<TInner, TKey, TResult>(
        IReadOnlyAsyncQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector
    ) =>
        Wrap(
            ((IQueryable<T>)this).GroupJoin(
                GetSource<TInner>(inner, nameof(inner)),
                outerKeySelector,
                innerKeySelector,
                resultSelector
            )
        );

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncQueryable<T>.LeftJoin<TInner, TKey, TResult>(
        IReadOnlyAsyncQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector
    ) =>
        Wrap(
            ApplyLeftJoin(
                GetSource<TInner>(inner, nameof(inner)),
                outerKeySelector,
                innerKeySelector,
                resultSelector
            )
        );

    IReadOnlyAsyncOrderedQueryable<T> IReadOnlyAsyncQueryable<T>.OrderBy<TKey>(
        Expression<Func<T, TKey>> keySelector
    ) => Wrap(((IQueryable<T>)this).OrderBy(keySelector));

    IReadOnlyAsyncOrderedQueryable<T> IReadOnlyAsyncQueryable<T>.OrderByDescending<TKey>(
        Expression<Func<T, TKey>> keySelector
    ) => Wrap(((IQueryable<T>)this).OrderByDescending(keySelector));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncQueryable<T>.Skip(int count) =>
        Wrap(((IQueryable<T>)this).Skip(count));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncQueryable<T>.Take(int count) =>
        Wrap(((IQueryable<T>)this).Take(count));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncQueryable<T>.Distinct() =>
        Wrap(((IQueryable<T>)this).Distinct());

    IReadOnlyAsyncOrderedQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.ThenBy<TKey>(
        Expression<Func<T, TKey>> keySelector
    ) => Wrap(((IOrderedQueryable<T>)this).ThenBy(keySelector));

    IReadOnlyAsyncOrderedQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.ThenByDescending<TKey>(
        Expression<Func<T, TKey>> keySelector
    ) => Wrap(((IOrderedQueryable<T>)this).ThenByDescending(keySelector));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.Where(
        Expression<Func<T, bool>> predicate
    ) => Wrap(((IQueryable<T>)this).Where(predicate));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.Select<TResult>(
        Expression<Func<T, TResult>> selector
    ) => Wrap(((IQueryable<T>)this).Select(selector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.SelectMany<TResult>(
        Expression<Func<T, IEnumerable<TResult>>> selector
    ) => Wrap(((IQueryable<T>)this).SelectMany(selector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.SelectMany<
        TCollection,
        TResult
    >(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<T, TCollection, TResult>> resultSelector
    ) => Wrap(((IQueryable<T>)this).SelectMany(collectionSelector, resultSelector));

    IReadOnlyAsyncQueryable<IGrouping<TKey, T>> IReadOnlyAsyncOrderedQueryable<T>.GroupBy<TKey>(
        Expression<Func<T, TKey>> keySelector
    ) => Wrap(((IQueryable<T>)this).GroupBy(keySelector));

    IReadOnlyAsyncQueryable<IGrouping<TKey, TElement>> IReadOnlyAsyncOrderedQueryable<T>.GroupBy<
        TKey,
        TElement
    >(Expression<Func<T, TKey>> keySelector, Expression<Func<T, TElement>> elementSelector) =>
        Wrap(((IQueryable<T>)this).GroupBy(keySelector, elementSelector));

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.GroupJoin<
        TInner,
        TKey,
        TResult
    >(
        IReadOnlyAsyncOrderedQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, IEnumerable<TInner>, TResult>> resultSelector
    ) =>
        Wrap(
            ((IQueryable<T>)this).GroupJoin(
                GetSource<TInner>(inner, nameof(inner)),
                outerKeySelector,
                innerKeySelector,
                resultSelector
            )
        );

    IReadOnlyAsyncQueryable<TResult> IReadOnlyAsyncOrderedQueryable<T>.LeftJoin<
        TInner,
        TKey,
        TResult
    >(
        IReadOnlyAsyncOrderedQueryable<TInner> inner,
        Expression<Func<T, TKey>> outerKeySelector,
        Expression<Func<TInner, TKey>> innerKeySelector,
        Expression<Func<T, TInner, TResult>> resultSelector
    ) =>
        Wrap(
            ApplyLeftJoin(
                GetSource<TInner>(inner, nameof(inner)),
                outerKeySelector,
                innerKeySelector,
                resultSelector
            )
        );

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.Skip(int count) =>
        Wrap(((IQueryable<T>)this).Skip(count));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.Take(int count) =>
        Wrap(((IQueryable<T>)this).Take(count));

    IReadOnlyAsyncQueryable<T> IReadOnlyAsyncOrderedQueryable<T>.Distinct() =>
        Wrap(((IQueryable<T>)this).Distinct());
}
