using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace EverModern.DataProvision.Queryables;

class StrictlyAsyncQueryProvider : Microsoft.EntityFrameworkCore.Query.Internal.EntityQueryProvider, IQueryProvider, IAsyncQueryProvider
{
    readonly IAsyncQueryProvider _source;
    readonly Func<CancellationToken, ValueTask<IDisposable>> _lock;

    public StrictlyAsyncQueryProvider(IAsyncQueryProvider source, Func<CancellationToken, ValueTask<IDisposable>> @lock)
        : base(source.ExtractQueryCompiler())
    {
        _source = source;
        _lock = @lock;
    }

    public override IQueryable CreateQuery(Expression expression)
        => CreateQuery<object>(expression);

    public override IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new QueueQueryable<TElement>(_source.CreateQuery<TElement>(expression), _lock);

    public object? Execute(Expression expression)
        => throw new NotSupportedException("Only async calls should be used on this set.");

    public TResult Execute<TResult>(Expression expression)
        => throw new NotSupportedException("Only async calls should be used on this set.");

    public override TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var lockValueTask = _lock(cancellationToken);
        if (lockValueTask.IsCompleted)
        {
            var result = _source.ExecuteAsync<TResult>(expression, cancellationToken);
            if (result is Task resultTask)
            {
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler
                resultTask
                    .ContinueWith(_ => lockValueTask.Result.Dispose(), CancellationToken.None)
                    .GetType();
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler
            }
            else
            {
                throw new InvalidOperationException("Executed task did not return task.");
            }

            return result;
        }
        else
        {
            var lockTask = lockValueTask.AsTask();

            if (typeof(TResult) == typeof(Task))
            {
                return (TResult)(object)lockTask.ContinueWith(
                    async ltTask => await (_source.ExecuteAsync<TResult>(expression, cancellationToken) as Task).ContinueWith(_ => ltTask.Result.Dispose(), TaskScheduler.Current));
            }

            var resultTask = lockTask.ContinueWith((Func<Task<IDisposable>, Task<object>>)(async lt =>
            {
                var subresult = _source.ExecuteAsync<TResult>(expression, cancellationToken);
                if (subresult is Task subresultTask)
                {
                    subresultTask.ContinueWith((Action<Task>)(_ => lt.Result.Dispose())).GetType();
                    await subresultTask;
                    subresultTask.TryGetResult(out var subresultValue);
                    return subresultValue;
                }
                else
                {
                    throw new InvalidOperationException("Executed task did not return task.");
                }
            }));

            var taskResultType = typeof(TResult).GenericTypeArguments[0];

            var result = (object)QueryUtilities.MakeExactTaskReflectionMethod(resultTask, taskResultType);

            return (TResult)result;
        }
    }


}
