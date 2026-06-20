namespace EverModern.Events;

/// <summary>
/// Represents a synchronous notifier with subscription support.
/// </summary>
public interface INotifier
{
    /// <summary>
    /// Subscribes a parameterless handler.
    /// </summary>
    /// <param name="handler">The handler to invoke on notifications.</param>
    /// <returns>A subscription handle used to unsubscribe.</returns>
    Subscription Subscribe(Action handler);

    /// <summary>
    /// Subscribes a handler that receives its own <see cref="Subscription"/>.
    /// </summary>
    /// <param name="handler">The handler to invoke on notifications.</param>
    void Subscribe(Action<Subscription> handler)
    {
        Subscription subscription = null!;
        Action actualHandler = () => handler(subscription);
        subscription = Subscribe(actualHandler);
    }
}

/// <summary>
/// Represents a synchronous notifier that publishes values.
/// </summary>
/// <typeparam name="T">The published value type.</typeparam>
public interface INotifier<out T> : INotifier
{
    /// <summary>
    /// Subscribes a handler that receives published values.
    /// </summary>
    /// <param name="handler">The handler to invoke for each value.</param>
    /// <returns>A subscription handle used to unsubscribe.</returns>
    Subscription Subscribe(Action<T> handler);

    /// <summary>
    /// Subscribes a handler that receives published values and its own subscription handle.
    /// </summary>
    /// <param name="handler">The handler to invoke for each value.</param>
    void Subscribe(Action<T, Subscription> handler)
    {
        Subscription subscription = null!;
        Action<T> actualHandler = (x) => handler(x, subscription);
        subscription = Subscribe(actualHandler);
    }

    Subscription INotifier.Subscribe(Action handler)
    {
        var actualHandler = new Action<T>(_ => handler());
        return Subscribe(actualHandler);
    }

    void INotifier.Subscribe(Action<Subscription> handler)
    {
        Subscription subscription = null!;

        Action actualHandler = () => handler(subscription);

        subscription = Subscribe(actualHandler);
    }
}
