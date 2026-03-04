namespace EverModern.DataProvision;

/// <summary>
/// Represents a simple event subscription source.
/// </summary>
public interface IEventSubscriber
{
    /// <summary>
    /// Subscribes a callback to the event source.
    /// </summary>
    /// <param name="callback">The callback to invoke when the event fires.</param>
    /// <returns>A disposable subscription handle.</returns>
    SubscriptionHolder Subscribe(Action callback);

    /// <summary>
    /// Gets whether the event has been fired at least once.
    /// </summary>
    bool HasBeenFired { get; }
}
