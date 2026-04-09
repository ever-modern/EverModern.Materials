namespace EverModern.WheelProtection.DataStructures.Events;

public interface INotifier<out T>
{
    Subscription Subscribe(Action<T> handler);
}

public interface INotifier
{
    Subscription Subscribe(Action handler);
}
