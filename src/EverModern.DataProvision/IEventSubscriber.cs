namespace EverModern.DataProvision;

public interface IEventSubscriber
{
    SubscriptionHolder Subscribe(Action callback);

    bool HasBeenFired { get; }
}
