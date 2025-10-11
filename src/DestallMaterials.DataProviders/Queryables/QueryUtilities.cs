using Microsoft.EntityFrameworkCore.Query;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Collections.Concurrent;

namespace DestallMaterials.EnlightenedDataProvision.Queryables;

static class QueryUtilities
{
    static readonly FieldInfo QueryCompilerField = typeof(EntityQueryProvider)
        .GetField("_queryCompiler", BindingFlags.NonPublic | BindingFlags.Instance)!;
    public static IQueryCompiler ExtractQueryCompiler(this IAsyncQueryProvider source)
        => (IQueryCompiler)QueryCompilerField.GetValue(source)!;

    public static Task<TResult> MakeExactTask<TResult>(this Task<object> input)
        => input.ContinueWith(t => (TResult)t.Result, TaskScheduler.Current);

    public static readonly Func<Task, Type, Task> MakeExactTaskReflectionMethod =
        (task, resultType) => (Task)(typeof(QueryUtilities)
                .GetMethod(nameof(MakeExactTask), BindingFlags.Public | BindingFlags.Static) 
                    ?? throw new MissingMethodException($"The method {nameof(MakeExactTask)} has not been got."))
                .MakeGenericMethod([resultType])?
                .Invoke(null, [task]);

    public static Type GetTaskResultType(this Task task)
        => task.GetType().GenericTypeArguments[0];

    public static Task CreateTaskWithResultType(object taskResult, Type targetType = null)
    { 
        targetType = targetType ?? taskResult.GetType();
        var createTaskMethod = typeof(Task).GetMethod(nameof(Task.FromResult)).MakeGenericMethod(targetType);
        var result = createTaskMethod.Invoke(null, [taskResult]);
        return (Task)result;
    }

    static readonly ConcurrentDictionary<Type, Func<object, object>> _resultExtractors = new();
    public static bool TryGetResult(this Task task, out object result)
    {
        if (task.GetType() == typeof(Task))
        {
            result = null;
            return false;
        }

        var taskType = task.GetType();
        if (_resultExtractors.TryGetValue(taskType, out var resultExtractor))
        {
            result = resultExtractor(task);
            return true;
        }
        else 
        {
            Func<object, object> extractor = taskType.GetProperty("Result").GetValue;
            _resultExtractors[taskType] = extractor;
            result = extractor(task);

            return true;
        }
    }

    public static IQueryable<T> WithExpressionOf<T>(this IQueryable<T> recipient, IQueryable<T> donor)
        => recipient.Provider.CreateQuery<T>(donor.Expression);
}
