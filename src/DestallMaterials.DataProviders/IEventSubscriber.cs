namespace DestallMaterials.EnlightenedDataProvision;

public interface IEventSubscriber
{
    SubscriptionHolder Subscribe(Action callback);

    bool HasBeenFired { get; }
}
