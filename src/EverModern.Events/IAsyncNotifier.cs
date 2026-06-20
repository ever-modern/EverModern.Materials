namespace EverModern.Events;

/// <summary>
/// Represents an asynchronous notifier with subscription support.
/// </summary>
public interface IAsyncNotifier
{
    /// <summary>
    /// Subscribes a parameterless asynchronous handler.
    /// </summary>
    /// <param name="handler">The handler to invoke on notifications.</param>
    /// <returns>A subscription handle used to unsubscribe.</returns>
    Subscription Subscribe(Func<ValueTask> handler);

    /// <summary>
    /// Subscribes an asynchronous handler that receives its own <see cref="Subscription"/>.
    /// </summary>
    /// <param name="handler">The handler to invoke on notifications.</param>
    void Subscribe(Func<Subscription, ValueTask> handler)
    {
        Subscription subscription = null!;

        Func<ValueTask> actualHandler = () => handler(subscription);

        subscription = Subscribe(actualHandler);
    }
}

/// <summary>
/// Represents an asynchronous notifier that publishes values.
/// </summary>
/// <typeparam name="T">The published value type.</typeparam>
public interface IAsyncNotifier<out T> : IAsyncNotifier
{
    /// <summary>
    /// Subscribes an asynchronous handler that receives published values.
    /// </summary>
    /// <param name="handler">The handler to invoke for each value.</param>
    /// <returns>A subscription handle used to unsubscribe.</returns>
    Subscription Subscribe(Func<T, ValueTask> handler);

    /// <summary>
    /// Subscribes an asynchronous handler that receives values and its own subscription.
    /// </summary>
    /// <param name="handler">The handler to invoke for each value.</param>
    void Subscribe(Func<T, Subscription, ValueTask> handler)
    {
        Subscription subscription = null!;

        Func<T, ValueTask> actualHandler = x => handler(x, subscription);

        subscription = Subscribe(actualHandler);
    }

    Subscription IAsyncNotifier.Subscribe(Func<ValueTask> handler)
    {
        var actualHandler = new Func<T, ValueTask>(_ => handler());
        return Subscribe(actualHandler);
    }

    void IAsyncNotifier.Subscribe(Func<Subscription, ValueTask> handler)
    {
        Subscription subscription = null!;

        Func<ValueTask> actualHandler = () => handler(subscription);

        subscription = Subscribe(actualHandler);
    }
}
