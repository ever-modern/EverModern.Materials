using System.Runtime.CompilerServices;

namespace DestallMaterials.WheelProtection.Extensions.Tasks;

public static class TaskExtensions
{
    public static Task<T> DefaultOnError<T>(this Task<T> task) =>
        task.ContinueWith(t => t.IsFaulted ? default : t.Result);

    public static Task IgnoreError(this Task task) => task.ContinueWith(t => { });

    public static Task IgnoreError<TException>(this Task task)
        where TException : Exception => task.ContinueWith(t =>
        {
            var error = t.Exception;
            if (error is not null && error is not TException)
            {
                throw error;
            }
        });

    public static async Task IgnoreErrors(this IEnumerable<Task> tasks)
    {
        foreach (var task in tasks)
        {
            await task.IgnoreError();
        }
    }

    public static async Task IgnoreErrors<T>(this IEnumerable<Task<T>> tasks)
    {
        foreach (var task in tasks)
        {
            await task.IgnoreError();
        }
    }

    public static Task<T> WithinDeadline<T>(this Task<T> task, TimeSpan deadline) =>
        Task.WhenAny(task, Task.Delay(deadline))
            .ContinueWith(_ =>
                !task.IsCompleted
                    ? throw new TimeoutException(
                        $"Task is not completed within period of {deadline}."
                    )
                    : task.Result
            );

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

    public static async Task<TOut> Then<TIn, TOut>(
        this Task<TIn> task,
        Func<TIn, Task<TOut>> asyncSelector
    )
    {
        var res1 = await task;
        var res2 = await asyncSelector(res1);

        return res2;
    }

    public static async Task<TOut> Then<TIn, TOut>(this Task<TIn> task, Func<TIn, TOut> selector)
    {
        var res1 = await task;
        var res2 = selector(res1);

        return res2;
    }

    public static async Task Then<TIn>(this Task<TIn> task, Action<TIn> selector)
    {
        var res1 = await task;
        selector(res1);
    }

    public static async Task Then<TIn, TOut>(this Task<TIn> task, Action<TIn> action)
    {
        var res1 = await task;
        action(res1);
    }

    public static async Task ThenAsync<TIn>(this Task<TIn> task, Func<TIn, Task> asyncAction)
    {
        var res1 = await task;
        await asyncAction(res1);
    }

    public static async Task<TOut> ThenAsync<TIn, TOut>(
        this Task task,
        Func<Task<TOut>> asyncSelector
    )
    {
        await task;
        var res2 = await asyncSelector();

        return res2;
    }

    public static async Task<TOut> Then<TOut>(this Task task, Func<TOut> selector)
    {
        await task;
        var res2 = selector();

        return res2;
    }

    public static async Task Then(this Task task, Action action)
    {
        await task;
        action();
    }

    public static async Task ThenAsync(this Task task, Func<Task> asyncAction)
    {
        await task;
        await asyncAction();
    }

    public static async Task<TOut> ThenAsync<TIn, TOut>(this Task<TIn> task, Func<TIn, Task<TOut>> asyncAction)
    {
        var prev = await task;
        var result = await asyncAction(prev);
        return result;
    }

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

    public static void Forget(this Task _) { }

    public static TaskAwaiter<ValueTuple<T1, T2>> GetAwaiter<T1, T2>(
        this ValueTuple<Task<T1>, Task<T2>> tasksTuple
    ) => tasksTuple.Item1.Then(r1 => tasksTuple.Item2.Then(r2 => (r1, r2))).GetAwaiter();

    public static TaskAwaiter<(T1, T2, T3)> GetAwaiter<T1, T2, T3>(
        this (Task<T1>, Task<T2>, Task<T3>) tasks
    ) =>
        Task.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3)
            .ContinueWith(_ => (tasks.Item1.Result, tasks.Item2.Result, tasks.Item3.Result))
            .GetAwaiter();

    public static TaskAwaiter<(T1, T2, T3, T4)> GetAwaiter<T1, T2, T3, T4>(
        this (Task<T1>, Task<T2>, Task<T3>, Task<T4>) tasks
    ) =>
        Task.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3, tasks.Item4)
            .ContinueWith(_ =>
                (tasks.Item1.Result, tasks.Item2.Result, tasks.Item3.Result, tasks.Item4.Result)
            )
            .GetAwaiter();

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
