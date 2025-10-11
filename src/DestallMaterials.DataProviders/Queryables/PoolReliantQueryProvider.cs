

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using DestallMaterials.EnlightenedDataProvision;

namespace DestallMaterials.EnlightenedDataProvision.Queryables;

class PoolReliantQueryProvider<TDbContext>
    : Microsoft.EntityFrameworkCore.Query.Internal.EntityQueryProvider,
        IQueryProvider,
        IAsyncQueryProvider
    where TDbContext : DbContext
{
    readonly IAsyncQueryProvider _source;
    readonly ExpressionExecutingDbContextFactory<TDbContext> _dbContextFactory;
    readonly Func<TDbContext, IQueryable> _rootQueryFactory;
    readonly ExecuteChanges<TDbContext> _executeChanges;

    public PoolReliantQueryProvider(
        IAsyncQueryProvider source,
        ExpressionExecutingDbContextFactory<TDbContext> dbContextFactory,
        Func<TDbContext, IQueryable> rootQueryFactory,
        ExecuteChanges<TDbContext> executeChanges)
        : base(source.ExtractQueryCompiler())
    {
        _source = source;
        _dbContextFactory = dbContextFactory;
        _rootQueryFactory = rootQueryFactory;
        _executeChanges = executeChanges;
    }

    public override object Execute(Expression expression)
        => throw new NotSupportedException("Only async calls should be used on this set type.");

    public override TResult Execute<TResult>(Expression expression)
        => throw new NotSupportedException("Only async calls should be used on this set type.");

    static readonly MethodInfo ExecuteExpressionMethod = typeof(PoolReliantQueryProvider<TDbContext>)
        .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(m => m.Name.Contains(nameof(ExecuteExpressionAsync)))
        ?? throw new MissingMethodException();

    async Task<T> ExecuteExpressionAsync<T>(Expression expression, CancellationToken cancellationToken)
    {
        bool makesChanges = (expression as MethodCallExpression)?.Method.Name is "ExecuteUpdate" or "ExecuteDelete";

        using var locker = await _dbContextFactory(makesChanges, cancellationToken);
        var dbContext = locker.Item;

        var rootQuery = _rootQueryFactory(dbContext);
        var replacedProvider = (IAsyncQueryProvider)rootQuery.Provider;

        if (makesChanges is false)
        {
            var noChangeResult = await (replacedProvider.ExecuteAsync<Task<T>>(expression, cancellationToken) as Task<T>
                ?? throw new InvalidOperationException("Executed task did not return task."));
            return noChangeResult;
        }

        T result = default;
        await _executeChanges(
            async (ct) =>
            {
                var resultTask = replacedProvider.ExecuteAsync<Task<T>>(expression, ct) as Task<T> 
                    ?? throw new InvalidOperationException("Executed task did not return task.");
                result = await resultTask;
            },
            locker,
            cancellationToken);

        return result!;
    }

    public override TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var taskResultType = typeof(TResult).GenericTypeArguments[0];

        var resultTask = (TResult)ExecuteExpressionMethod
            .MakeGenericMethod(taskResultType)
            .Invoke(this, [expression, cancellationToken])!;

        return resultTask;
    }

    public override IQueryable CreateQuery(Expression expression)
        => CreateQuery<object>(expression);

    public override IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new PoolReliantQueryable<TElement, TDbContext>(
            _source.CreateQuery<TElement>(expression),
            dbContext => (IQueryable<TElement>)_rootQueryFactory(dbContext).Provider.CreateQuery(expression),
            _dbContextFactory,
            _executeChanges);
}
