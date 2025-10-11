using System.Linq.Expressions;
using System.Collections;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using DestallMaterials.WheelProtection.Queues;

namespace DestallMaterials.EnlightenedDataProvision.Queryables;


public delegate ValueTask<ItemLocker<IQueryable<T>>> QueryableFactory<T>(CancellationToken cancellationToken);
public delegate ValueTask<ItemLocker<IQueryable>> QueryableFactory(Type itemType, CancellationToken cancellationToken);
public delegate Task ExecuteChanges<TDbContext>(
        Func<CancellationToken, Task> executeExpression,
        ItemLocker<TDbContext> dbContextLocker,
        CancellationToken cancellationToken);

public class PoolReliantQueryable<T, TDbContext> : IQueryable<T>, IAsyncEnumerable<T>, IOrderedQueryable<T>
    where TDbContext : DbContext
{
    readonly ExpressionExecutingDbContextFactory<TDbContext> _dbContextFactory;
    readonly IQueryable<T> _currentQuery;
    readonly Func<TDbContext, IQueryable<T>> _rootQueryFactory;

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

    public static PoolReliantQueryable<T, TDbContext> Create(
        IQueryable<T> currentQuery,
        Func<TDbContext, IQueryable<T>> rootQueryFactory,
        ExpressionExecutingDbContextFactory<TDbContext> dbContextFactory,
        ExecuteChanges<TDbContext> executeChanges) => new(currentQuery, rootQueryFactory, dbContextFactory, executeChanges);

    public Type ElementType => typeof(T);

    public Expression Expression { get; }

    public IQueryProvider Provider { get; }


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

    public IEnumerator<T> GetEnumerator()
        => throw new NotSupportedException("Only async calls are supported on this IQueryable");

    IEnumerator IEnumerable.GetEnumerator()
        => throw new NotSupportedException("Only async calls are supported on this IQueryable");
}
