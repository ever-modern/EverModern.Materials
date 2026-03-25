namespace EverModern.DataProvision;

/// <summary>
/// Provides a simple in-memory event dispatcher with subscription support.
/// </summary>
public class EventManager : IEventSubscriber
{
    /// <inheritdoc />
    public bool HasBeenFired { get; private set; }

    readonly List<Action> _subscriptions = [];

    /// <inheritdoc />
    public SubscriptionHolder Subscribe(Action callback)
    {
        lock (_subscriptions)
        {
            var result = new SubscriptionHolder(() => _subscriptions.Remove(callback));
            _subscriptions.Add(callback);
            return result;
        }
    }

    /// <summary>
    /// Fires the event and invokes all registered callbacks.
    /// </summary>
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