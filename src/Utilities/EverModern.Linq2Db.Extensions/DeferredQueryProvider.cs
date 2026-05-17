using System.Linq.Expressions;
using System.Reflection;
using LinqToDB;

namespace EverModern.QueryKit;

sealed class DeferredQueryProvider(
    IQueryProvider source,
    Func<IDataContext, IQueryable> rootQueryFactory,
    ConnectionFactory<IDataContext> dataConnectionFactory
) : IQueryProvider
{
    static readonly Dictionary<Type, MethodInfo> _executeAsyncMethods = [];

    public IQueryable CreateQuery(Expression expression) => CreateQuery<object>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new DeferredQueryable<TElement>(
            currentQuery: source.CreateQuery<TElement>(expression),
            rootQueryFactory: dataContext =>
                (IQueryable<TElement>)
                    rootQueryFactory(dataContext).Provider.CreateQuery(expression),
            dataConnectionFactory: dataConnectionFactory
        );

    public object? Execute(Expression expression) =>
        throw new NotSupportedException("Only async calls are supported on this IQueryable.");

    public TResult Execute<TResult>(Expression expression) =>
        throw new NotSupportedException("Only async calls are supported on this IQueryable.");

    static readonly MethodInfo ExecuteAsyncTaskGenericMethod = typeof(DeferredQueryProvider)
        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
        .First(m => m.Name == nameof(ExecuteAsyncTaskGeneric));

    public TResult ExecuteAsync<TResult>(
        Expression expression,
        CancellationToken cancellationToken = default
    )
    {
        var resultType = typeof(TResult);

        if (resultType == typeof(Task))
        {
            return (TResult)(object)ExecuteAsyncTask(expression, cancellationToken);
        }

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var taskResultType = resultType.GetGenericArguments()[0];
            var taskResult =
                ExecuteAsyncTaskGenericMethod
                    .MakeGenericMethod(taskResultType)
                    .Invoke(this, [expression, cancellationToken])
                ?? throw new InvalidOperationException("Could not execute async query.");

            return (TResult)taskResult;
        }

        throw new NotSupportedException($"Async execution type {resultType} is not supported.");
    }

    async Task ExecuteAsyncTask(Expression expression, CancellationToken cancellationToken)
    {
        await using var dataConnection = await dataConnectionFactory(cancellationToken);
        var rootQuery = rootQueryFactory(dataConnection);

        var asyncResult = InvokeExecuteAsync(
            rootQuery.Provider,
            typeof(Task),
            expression,
            cancellationToken
        );
        if (asyncResult is not Task resultTask)
        {
            throw new InvalidOperationException(
                "The query provider did not return Task for async execution."
            );
        }

        await resultTask;
    }

    async Task<TResult> ExecuteAsyncTaskGeneric<TResult>(
        Expression expression,
        CancellationToken cancellationToken
    )
    {
        await using var dataConnection = await dataConnectionFactory(cancellationToken);
        var rootQuery = rootQueryFactory(dataConnection);

        var asyncResult = InvokeExecuteAsync(
            rootQuery.Provider,
            typeof(Task<TResult>),
            expression,
            cancellationToken
        );
        if (asyncResult is not Task<TResult> resultTask)
        {
            throw new InvalidOperationException(
                "The query provider did not return Task<TResult> for async execution."
            );
        }

        return await resultTask;
    }

    static object InvokeExecuteAsync(
        IQueryProvider queryProvider,
        Type executeResultType,
        Expression expression,
        CancellationToken cancellationToken
    )
    {
        var providerType = queryProvider.GetType();
        if (_executeAsyncMethods.TryGetValue(providerType, out var executeAsyncMethod) is false)
        {
            executeAsyncMethod =
                providerType
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(m => m.Name == nameof(ExecuteAsync))
                    .Where(m => m.IsGenericMethodDefinition)
                    .FirstOrDefault(m =>
                    {
                        var parameters = m.GetParameters();
                        return parameters.Length == 2
                            && parameters[0].ParameterType == typeof(Expression)
                            && parameters[1].ParameterType == typeof(CancellationToken);
                    })
                ?? throw new NotSupportedException(
                    $"Provider {queryProvider.GetType()} does not support ExecuteAsync."
                );

            _executeAsyncMethods[providerType] = executeAsyncMethod;
        }

        return executeAsyncMethod
                .MakeGenericMethod(executeResultType)
                .Invoke(queryProvider, [expression, cancellationToken])
            ?? throw new InvalidOperationException("Provider returned null for ExecuteAsync.");
    }
}
