using System.Diagnostics;
using EverModern.WheelProtection.DataStructures;

namespace EverModern.WheelProtection.Executions;

/// <summary>
/// Provides helper methods for resilient execution, retrying, and timing.
/// </summary>
public static class Please
{
    static async Task Delay(TimeSpan delay, CancellationToken ct)
    {
        if (delay <= TimeSpan.Zero)
        {
            await Task.Yield();
            return;
        }

        await Task.Delay(delay, ct);
    }

    // =========================
    // RETRY CORE
    // =========================

    static async Task<TResult> RetryCore<TResult>(
        Func<Task<TResult>> func,
        int maxTries,
        Func<TResult, bool>? validator,
        TimeSpan delay,
        CancellationToken ct
    )
    {
        if (func is null)
            throw new ArgumentNullException(nameof(func));
        if (maxTries <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxTries));

        Exception? lastException = null;

        while (maxTries-- > 0)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var result = await func();

                if (validator is null || validator(result))
                    return result;

                if (maxTries == 0)
                    return result;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (maxTries == 0)
                    throw;
            }

            await Delay(delay, ct);
        }

        throw lastException ?? new InvalidOperationException("Retry failed.");
    }

    // =========================
    // RETRIES
    // =========================

    public static Task<TResult> RepeatUntilSuccessAsync<TResult>(
        this Func<Task<TResult>> func,
        int maxTriesCount,
        TimeSpan awaitBetweenTries = default,
        CancellationToken ct = default
    ) => RetryCore(func, maxTriesCount, null, awaitBetweenTries, ct);

    public static Task<TResult> RepeatUntilSuccessAsync<TResult>(
        this Func<Task<TResult>> func,
        int maxTriesCount,
        Func<TResult, bool> validityCriterion,
        TimeSpan awaitBetweenTries = default,
        CancellationToken ct = default
    ) => RetryCore(func, maxTriesCount, validityCriterion, awaitBetweenTries, ct);

    // =========================
    // TIMING CORE
    // =========================

    static TResult MeasureCore<TResult>(Func<TResult> func, Action<TimeSpan> callback)
    {
        if (func is null)
            throw new ArgumentNullException(nameof(func));
        if (callback is null)
            throw new ArgumentNullException(nameof(callback));

        var sw = Stopwatch.StartNew();
        try
        {
            return func();
        }
        finally
        {
            callback(sw.Elapsed);
        }
    }

    static async Task<TResult> MeasureCoreAsync<TResult>(
        Func<Task<TResult>> func,
        Action<TimeSpan> callback
    )
    {
        if (func is null)
            throw new ArgumentNullException(nameof(func));
        if (callback is null)
            throw new ArgumentNullException(nameof(callback));

        var sw = Stopwatch.StartNew();
        try
        {
            return await func();
        }
        finally
        {
            callback(sw.Elapsed);
        }
    }

    static async Task MeasureCoreAsync(Func<Task> func, Action<TimeSpan> callback)
    {
        if (func is null)
            throw new ArgumentNullException(nameof(func));
        if (callback is null)
            throw new ArgumentNullException(nameof(callback));

        var sw = Stopwatch.StartNew();
        try
        {
            await func();
        }
        finally
        {
            callback(sw.Elapsed);
        }
    }

    // =========================
    // TIMING API
    // =========================

    public static TResult MeasureExecutionTime<TResult>(
        this Func<TResult> func,
        Action<TimeSpan> doWithTimeTaken
    ) => MeasureCore(func, doWithTimeTaken);

    public static Task<TResult> MeasureExecutionTimeAsync<TResult>(
        this Func<Task<TResult>> func,
        Action<TimeSpan> doWithTimeTaken
    ) => MeasureCoreAsync(func, doWithTimeTaken);

    public static Task MeasureExecutionTimeAsync(
        this Func<Task> func,
        Action<TimeSpan> doWithTimeTaken
    ) => MeasureCoreAsync(func, doWithTimeTaken);

    public static async Task<T> MeasureExecutionTimeAsync<T>(
        this Task<T> task,
        Action<TimeSpan> doWithTimeTaken
    )
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));
        if (doWithTimeTaken is null)
            throw new ArgumentNullException(nameof(doWithTimeTaken));

        var sw = Stopwatch.StartNew();
        try
        {
            return await task;
        }
        finally
        {
            doWithTimeTaken(sw.Elapsed);
        }
    }

    public static void MeasureExecutionTime(this Action action, Action<TimeSpan> doWithTimeTaken)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));
        if (doWithTimeTaken is null)
            throw new ArgumentNullException(nameof(doWithTimeTaken));

        var sw = Stopwatch.StartNew();
        try
        {
            action();
        }
        finally
        {
            doWithTimeTaken(sw.Elapsed);
        }
    }

    // =========================
    // ERROR WRAPPERS
    // =========================

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

    // =========================
    // RESULT WRAPPERS
    // =========================

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

    public static async Task<Result<T, TException>> RunAsync<T, TException>(
        this Func<Task<T>> asyncFunc
    )
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

    public static Exception? Run(this Action action)
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

    public static async Task<Exception?> RunAsync(this Func<Task> asyncTask)
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
