using System.Runtime.CompilerServices;

namespace EverModern.WheelProtection.Extensions.Tasks;

/// <summary>
/// Provides convenience extensions for tasks.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Returns the task result or default on error.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="task">The task.</param>
    public static Task<T> DefaultOnError<T>(this Task<T> task) =>
        task.ContinueWith(t => t.IsFaulted ? default : t.Result);

    /// <summary>
    /// Ignores any errors from a task.
    /// </summary>
    /// <param name="task">The task.</param>
    public static Task IgnoreError(this Task task) => task.ContinueWith(t => { });

    /// <summary>
    /// Ignores a specific exception type from a task.
    /// </summary>
    /// <typeparam name="TException">The exception type to ignore.</typeparam>
    /// <param name="task">The task.</param>
    public static Task IgnoreError<TException>(this Task task)
        where TException : Exception => task.ContinueWith(t =>
        {
            var error = t.Exception;
            if (error is not null && error is not TException)
            {
                throw error;
            }
        });

    /// <summary>
    /// Ignores errors from a sequence of tasks.
    /// </summary>
    /// <param name="tasks">The tasks.</param>
    public static async Task IgnoreErrors(this IEnumerable<Task> tasks)
    {
        foreach (var task in tasks)
        {
            await task.IgnoreError();
        }
    }

    /// <summary>
    /// Ignores errors from a sequence of tasks.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="tasks">The tasks.</param>
    public static async Task IgnoreErrors<T>(this IEnumerable<Task<T>> tasks)
    {
        foreach (var task in tasks)
        {
            await task.IgnoreError();
        }
    }

    /// <summary>
    /// Enforces a deadline for a task.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="task">The task.</param>
    /// <param name="deadline">The deadline.</param>
    public static Task<T> WithinDeadline<T>(this Task<T> task, TimeSpan deadline) =>
        Task.WhenAny(task, Task.Delay(deadline))
            .ContinueWith(_ =>
                !task.IsCompleted
                    ? throw new TimeoutException(
                        $"Task is not completed within period of {deadline}."
                    )
                    : task.Result
            );

    /// <summary>
    /// Enforces a deadline for a task.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <param name="deadline">The deadline.</param>
    public static Task WithinDeadline(this Task task, TimeSpan deadline) =>
        Task.WhenAny(task, Task.Delay(deadline))
            .ContinueWith(_ =>
            {
                if (!task.IsCompleted)
                {
                    throw new TimeoutException(
                        $"Task is not completed within period of {deadline}."
                    );
                }
            });

    /// <summary>
    /// Continues with an async selector.
    /// </summary>
    public static async Task<TOut> Then<TIn, TOut>(
        this Task<TIn> task,
        Func<TIn, Task<TOut>> asyncSelector
    )
    {
        var res1 = await task;
        var res2 = await asyncSelector(res1);

        return res2;
    }

    /// <summary>
    /// Continues with a selector.
    /// </summary>
    public static async Task<TOut> Then<TIn, TOut>(this Task<TIn> task, Func<TIn, TOut> selector)
    {
        var res1 = await task;
        var res2 = selector(res1);

        return res2;
    }

    /// <summary>
    /// Continues with an action.
    /// </summary>
    public static async Task Then<TIn>(this Task<TIn> task, Action<TIn> selector)
    {
        var res1 = await task;
        selector(res1);
    }

    /// <summary>
    /// Continues with an action.
    /// </summary>
    public static async Task Then<TIn, TOut>(this Task<TIn> task, Action<TIn> action)
    {
        var res1 = await task;
        action(res1);
    }

    /// <summary>
    /// Continues with an async action.
    /// </summary>
    public static async Task ThenAsync<TIn>(this Task<TIn> task, Func<TIn, Task> asyncAction)
    {
        var res1 = await task;
        await asyncAction(res1);
    }

    /// <summary>
    /// Continues a task with an async selector.
    /// </summary>
    public static async Task<TOut> ThenAsync<TIn, TOut>(
        this Task task,
        Func<Task<TOut>> asyncSelector
    )
    {
        await task;
        var res2 = await asyncSelector();

        return res2;
    }

    /// <summary>
    /// Continues a task with a selector.
    /// </summary>
    public static async Task<TOut> Then<TOut>(this Task task, Func<TOut> selector)
    {
        await task;
        var res2 = selector();

        return res2;
    }

    /// <summary>
    /// Continues a task with an action.
    /// </summary>
    public static async Task Then(this Task task, Action action)
    {
        await task;
        action();
    }

    /// <summary>
    /// Continues a task with an async action.
    /// </summary>
    public static async Task ThenAsync(this Task task, Func<Task> asyncAction)
    {
        await task;
        await asyncAction();
    }

    /// <summary>
    /// Continues a task with an async selector.
    /// </summary>
    public static async Task<TOut> ThenAsync<TIn, TOut>(this Task<TIn> task, Func<TIn, Task<TOut>> asyncAction)
    {
        var prev = await task;
        var result = await asyncAction(prev);
        return result;
    }

    /// <summary>
    /// Handles task errors and returns result or error.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="task">The task.</param>
    public static ValueTask<(TResult? Result, AggregateException? Error)> HandleError<TResult>(
        this Task<TResult> task
    )
    {
        if (task.IsCompleted)
        {
            if (task.IsFaulted)
            {
                return new((default, task.Exception!));
            }
            else
            {
                return new((task.Result, null));
            }
        }
        else
        {
            return new(
                task.ContinueWith(t =>
                    t.IsFaulted ? (default(TResult), t.Exception) : (t.Result, t.Exception)
                )
            );
        }
    }

    /// <summary>
    /// Intentionally ignores a task.
    /// </summary>
    /// <param name="_">The task.</param>
    public static void Forget(this Task _) { }

    /// <summary>
    /// Enables awaiting a tuple of two tasks.
    /// </summary>
    public static TaskAwaiter<ValueTuple<T1, T2>> GetAwaiter<T1, T2>(
        this ValueTuple<Task<T1>, Task<T2>> tasksTuple
    ) => tasksTuple.Item1.Then(r1 => tasksTuple.Item2.Then(r2 => (r1, r2))).GetAwaiter();

    /// <summary>
    /// Enables awaiting a tuple of three tasks.
    /// </summary>
    public static TaskAwaiter<(T1, T2, T3)> GetAwaiter<T1, T2, T3>(
        this (Task<T1>, Task<T2>, Task<T3>) tasks
    ) =>
        Task.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3)
            .ContinueWith(_ => (tasks.Item1.Result, tasks.Item2.Result, tasks.Item3.Result))
            .GetAwaiter();

    /// <summary>
    /// Enables awaiting a tuple of four tasks.
    /// </summary>
    public static TaskAwaiter<(T1, T2, T3, T4)> GetAwaiter<T1, T2, T3, T4>(
        this (Task<T1>, Task<T2>, Task<T3>, Task<T4>) tasks
    ) =>
        Task.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4)
            .ContinueWith(_ =>
                (tasks.Item1.Result, tasks.Item2.Result, tasks.Item3.Result, tasks.Item4.Result)
            )
            .GetAwaiter();

    /// <summary>
    /// Enables awaiting a tuple of five tasks.
    /// </summary>
    public static TaskAwaiter<(T1, T2, T3, T4, T5)> GetAwaiter<T1, T2, T3, T4, T5>(
        this (Task<T1>, Task<T2>, Task<T3>, Task<T4>, Task<T5>) tasks
    ) =>
        Task.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4, tasks.Item5)
            .ContinueWith(_ =>
                (
                    tasks.Item1.Result,
                    tasks.Item2.Result,
                    tasks.Item3.Result,
                    tasks.Item4.Result,
                    tasks.Item5.Result
                )
            )
            .GetAwaiter();

    /// <summary>
    /// Enables awaiting a tuple of six tasks.
    /// </summary>
    public static TaskAwaiter<(T1, T2, T3, T4, T5, T6)> GetAwaiter<T1, T2, T3, T4, T5, T6>(
        this (Task<T1>, Task<T2>, Task<T3>, Task<T4>, Task<T5>, Task<T6>) tasks
    ) =>
        Task.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4, tasks.Item5, tasks.Item6)
            .ContinueWith(_ =>
                (
                    tasks.Item1.Result,
                    tasks.Item2.Result,
                    tasks.Item3.Result,
                    tasks.Item4.Result,
                    tasks.Item5.Result,
                    tasks.Item6.Result
                )
            )
            .GetAwaiter();

    /// <summary>
    /// Enables awaiting a tuple of seven tasks.
    /// </summary>
    public static TaskAwaiter<(T1, T2, T3, T4, T5, T6, T7)> GetAwaiter_TaskWhenAll<
        T1,
        T2,
        T3,
        T4,
        T5,
        T6,
        T7
    >(this (Task<T1>, Task<T2>, Task<T3>, Task<T4>, Task<T5>, Task<T6>, Task<T7>) tasks) =>
        Task.WhenAll(
                tasks.Item1,
                tasks.Item2,
                tasks.Item3,
                tasks.Item4,
                tasks.Item5,
                tasks.Item6,
                tasks.Item7
            )
            .ContinueWith(_ =>
                (
                    tasks.Item1.Result,
                    tasks.Item2.Result,
                    tasks.Item3.Result,
                    tasks.Item4.Result,
                    tasks.Item5.Result,
                    tasks.Item6.Result,
                    tasks.Item7.Result
                )
            )
            .GetAwaiter();
}
