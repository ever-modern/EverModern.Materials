using System.Threading.Channels;

namespace EverModern.Threading.Channels;

/// <summary>
/// Represents a fan-out channel where written outgoing messages are distributed to subscriptions.
/// </summary>
/// <typeparam name="TIncomingMessage">The message type observed by subscribers.</typeparam>
/// <typeparam name="TOutgoingMessage">The message type accepted by the writer.</typeparam>
public interface IBroadcastChannel<TIncomingMessage, TOutgoingMessage>
{
    /// <summary>
    /// Gets the writer used to publish messages to the broadcast channel.
    /// </summary>
    ChannelWriter<TOutgoingMessage> Writer { get; }

    /// <summary>
    /// Creates a new subscription that receives messages matching the specified filter.
    /// </summary>
    /// <param name="filter">Predicate used to decide whether a message is yielded for this subscription.</param>
    /// <returns>A subscription that can asynchronously read matching messages.</returns>
    IChannelSubscription<TIncomingMessage> Subscribe(Func<TIncomingMessage, bool> filter);
}