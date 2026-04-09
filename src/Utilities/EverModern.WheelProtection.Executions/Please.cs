using EverModern.WheelProtection.DataStructures;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace EverModern.Threading.Executions;

/// <summary>
/// Provides helper methods for resilient execution and timing.
/// </summary>
public static class Please
{
    /// <summary>
    /// Repeats an async function until it succeeds or the retry limit is reached.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="function">The function to execute.</param>
    /// <param name="maxTriesCount">The maximum number of attempts.</param>
    /// <param name="awaitBetweenTries">The delay between attempts.</param>
    public static async Task<TResult> RepeatUntilSuccessAsync<TResult>(
        this Func<Task<TResult>> function,
        int maxTriesCount,
        TimeSpan awaitBetweenTries = default
    )
    {
        while (maxTriesCount-- > 0)
        {
            try
            {
                return await function();
            }
            catch
            {
                if (maxTriesCount == 0)
                {
                    throw;
                }
                await Task.Delay(awaitBetweenTries);
            }
        }
        return default;
    }

    /// <summary>
    /// Repeats an async function until it succeeds and passes a validity check.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="function">The function to execute.</param>
    /// <param name="maxTriesCount">The maximum number of attempts.</param>
    /// <param name="validityCriterion">The validity check for the result.</param>
    /// <param name="awaitBetweenTries">The delay between attempts.</param>
    public static async Task<TResult> RepeatUntilSuccessAsync<TResult>(
        this Func<Task<TResult>> function,
        int maxTriesCount,
        Func<TResult, bool> validityCriterion,
        TimeSpan awaitBetweenTries = default
    )
    {
        while (maxTriesCount-- > 0)
        {
            try
            {
                var result = await function();
                if (validityCriterion(result) || maxTriesCount == 0)
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                if (maxTriesCount == 0)
                {
                    throw;
                }
            }
            await Task.Delay(awaitBetweenTries);
        }
        return default;
    }

    /// <summary>
    /// Measures execution time for a synchronous function.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="doWithTimeTaken">Callback for the elapsed time.</param>
    public static TResult MeasureExecutionTime<TResult>(
        this Func<TResult> func,
        Action<TimeSpan> doWithTimeTaken
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = func();
            return result;
        }
        finally
        {
            doWithTimeTaken(stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Measures execution time for an async function.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="doWithTimeTaken">Callback for the elapsed time.</param>
    public static async Task<TResult> MeasureExecutionTimeAsync<TResult>(
        this Func<Task<TResult>> func,
        Action<TimeSpan> doWithTimeTaken
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await func();
            return result;
        }
        finally
        {
            doWithTimeTaken(stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Measures execution time for an async action.
    /// </summary>
    /// <param name="func">The action to execute.</param>
    /// <param name="doWithTimeTaken">Callback for the elapsed time.</param>
    public static async Task MeasureExecutionTimeAsync(
        this Func<Task> func,
        Action<TimeSpan> doWithTimeTaken
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await func();
        }
        finally
        {
            doWithTimeTaken(stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Measures execution time for a task.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="task">The task to execute.</param>
    /// <param name="doWithTimeTaken">Callback for the elapsed time.</param>
    public static async Task<T> MeasureExecutionTimeAsync<T>(this Task<T> task, Action<TimeSpan> doWithTimeTaken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await task;
        }
        finally
        {
            doWithTimeTaken(stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Measures execution time for a synchronous action.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="doWithTimeTaken">Callback for the elapsed time.</param>
    public static void MeasureExecutionTime(
        this Action action,
        Action<TimeSpan> doWithTimeTaken
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            action();
        }
        finally
        {
            stopwatch.Stop();
            doWithTimeTaken(stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Executes an async function and returns a fallback value on error.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="onError">The error handler.</param>
    public static async Task<TResult> ReturnOnErrorAsync<TResult>(
        this Func<Task<TResult>> func,
        Func<Exception, TResult> onError
    )
    {
        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            return onError(ex);
        }
    }

    /// <summary>
    /// Executes a function and returns a fallback value on error.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="onError">The error handler.</param>
    public static TResult ReturnOnError<TResult>(
        this Func<TResult> func,
        Func<Exception, TResult> onError
    )
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            return onError(ex);
        }
    }

    /// <summary>
    /// Executes an async function and returns a result wrapper.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="asyncFunc">The function to execute.</param>
    public static async Task<Result<T>> RunAsync<T>(this Func<Task<T>> asyncFunc)
    {
        try
        {
            return await asyncFunc();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Executes an async function and returns a result wrapper for a specific exception type.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="asyncFunc">The function to execute.</param>
    public static async Task<Result<T, TException>> RunAsync<T, TException>(this Func<Task<T>> asyncFunc)
        where TException : Exception
    {
        try
        {
            return await asyncFunc();
        }
        catch (TException ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Executes a function and returns a result wrapper.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="func">The function to execute.</param>
    public static Result<T> Run<T>(this Func<T> func)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Executes an action and returns the thrown exception, if any.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public static Exception Run(this Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Executes an async action and returns the thrown exception, if any.
    /// </summary>
    /// <param name="asyncTask">The async action to execute.</param>
    public static async Task<Exception> RunAsync(this Func<Task> asyncTask)
    {
        try
        {
            await asyncTask();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
