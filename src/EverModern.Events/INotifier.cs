namespace EverModern.Events;

public interface INotifier
{
    Subscription Subscribe(Action handler);

    void Subscribe(Action<Subscription> handler)
    {
        Subscription subscription = null!;
        Action actualHandler = () => handler(subscription);
        subscription = Subscribe(actualHandler);
    }
}

public interface INotifier<out T> : INotifier
{
    Subscription Subscribe(Action<T> handler);

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
