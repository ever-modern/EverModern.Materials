using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using EverModern.Threading.Queues;

namespace EverModern.DataProvision.Queryables;


/// <summary>
/// Creates a pooled queryable locker for a given element type.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <param name="cancellationToken">A cancellation token.</param>
/// <returns>A queryable locker.</returns>
public delegate ValueTask<ItemLocker<IQueryable<T>>> QueryableFactory<T>(CancellationToken cancellationToken);

/// <summary>
/// Creates a pooled queryable locker for a runtime element type.
/// </summary>
/// <param name="itemType">The element type.</param>
/// <param name="cancellationToken">A cancellation token.</param>
/// <returns>A queryable locker.</returns>
public delegate ValueTask<ItemLocker<IQueryable>> QueryableFactory(Type itemType, CancellationToken cancellationToken);

/// <summary>
/// Executes a query that may perform changes using a reserved DbContext.
/// </summary>
/// <typeparam name="TDbContext">The DbContext type.</typeparam>
/// <param name="executeExpression">The query execution callback.</param>
/// <param name="dbContextLocker">The reserved DbContext locker.</param>
/// <param name="cancellationToken">A cancellation token.</param>
public delegate Task ExecuteChanges<TDbContext>(
        Func<CancellationToken, Task> executeExpression,
        ItemLocker<TDbContext> dbContextLocker,
        CancellationToken cancellationToken);

/// <summary>
/// IQueryable implementation that relies on a pooled DbContext per async enumeration.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <typeparam name="TDbContext">The DbContext type.</typeparam>
public class PoolReliantQueryable<T, TDbContext> : IQueryable<T>, IAsyncEnumerable<T>, IOrderedQueryable<T>
    where TDbContext : DbContext
{
    readonly ExpressionExecutingDbContextFactory<TDbContext> _dbContextFactory;
    readonly IQueryable<T> _currentQuery;
    readonly Func<TDbContext, IQueryable<T>> _rootQueryFactory;

    /// <summary>
    /// Initializes a new instance of the pool-reliant queryable.
    /// </summary>
    /// <param name="currentQuery">The current query expression.</param>
    /// <param name="rootQueryFactory">The root query factory.</param>
    /// <param name="dbContextFactory">The pooled context factory.</param>
    /// <param name="executeChanges">The change execution callback.</param>
    public PoolReliantQueryable(
        IQueryable<T> currentQuery,
        Func<TDbContext, IQueryable<T>> rootQueryFactory,
        ExpressionExecutingDbContextFactory<TDbContext> dbContextFactory,
        ExecuteChanges<TDbContext> executeChanges)
    {
        _dbContextFactory = dbContextFactory;
        _currentQuery = currentQuery;

        Provider = new PoolReliantQueryProvider<TDbContext>(
            source: (IAsyncQueryProvider)currentQuery.Provider,
            dbContextFactory: dbContextFactory,
            rootQueryFactory: rootQueryFactory,
            executeChanges: executeChanges);

        Expression = currentQuery.Expression;
        _rootQueryFactory = rootQueryFactory;
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Creates a new instance of the pool-reliant queryable.
    /// </summary>
    /// <param name="currentQuery">The current query expression.</param>
    /// <param name="rootQueryFactory">The root query factory.</param>
    /// <param name="dbContextFactory">The pooled context factory.</param>
    /// <param name="executeChanges">The change execution callback.</param>
    public static PoolReliantQueryable<T, TDbContext> Create(
        IQueryable<T> currentQuery,
        Func<TDbContext, IQueryable<T>> rootQueryFactory,
        ExpressionExecutingDbContextFactory<TDbContext> dbContextFactory,
        ExecuteChanges<TDbContext> executeChanges) => new(currentQuery, rootQueryFactory, dbContextFactory, executeChanges);

    /// <inheritdoc />
    public Type ElementType => typeof(T);

    /// <inheritdoc />
    public Expression Expression { get; }

    /// <inheritdoc />
    public IQueryProvider Provider { get; }

    /// <inheritdoc />
    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        using var dbContextLocker = await _dbContextFactory(false, cancellationToken);
        var dbContext = dbContextLocker.Item;
        var rootQuery = _rootQueryFactory(dbContext);
        var replacedQuery = rootQuery.Provider.CreateQuery<T>(_currentQuery.Expression);

        await foreach (var item in ((IAsyncEnumerable<T>)replacedQuery).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
        => throw new NotSupportedException("Only async calls are supported on this IQueryable");

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
        => throw new NotSupportedException("Only async calls are supported on this IQueryable");
}
