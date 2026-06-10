namespace EverModern.Events;

public interface IAsyncNotifier
{
    Subscription Subscribe(Func<ValueTask> handler);

    void Subscribe(Func<Subscription, ValueTask> handler)
    {
        Subscription subscription = null!;

        Func<ValueTask> actualHandler = () => handler(subscription);

        subscription = Subscribe(actualHandler);
    }
}

public interface IAsyncNotifier<out T> : IAsyncNotifier
{
    Subscription Subscribe(Func<T, ValueTask> handler);

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
