namespace DestallMaterials.EnlightenedDataProvision;

public class EventManager : IEventSubscriber
{
    public bool HasBeenFired { get; private set; }

    readonly List<Action> _subscriptions = [];

    public SubscriptionHolder Subscribe(Action callback)
    {
        lock (_subscriptions)
        {
            var result = new SubscriptionHolder(() => _subscriptions.Remove(callback));
            _subscriptions.Add(callback);
            return result;
        }
    }

    public void Fire()
    {
        lock (_subscriptions)
        {
            HasBeenFired = true;
            var l = _subscriptions.Count;
            for (int i = 0; i < l; i++)
            {
                var callback = _subscriptions[i];
                try
                {
                    callback();
                }
                catch { }
            }
        }
    }
}