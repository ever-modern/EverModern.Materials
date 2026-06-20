namespace EverModern.Threading.Channels;

/// <summary>
/// Represents a subscription to a broadcast channel.
/// </summary>
/// <typeparam name="T">The message type produced by the subscription.</typeparam>
public interface IChannelSubscription<out T> : IDisposable
{
    /// <summary>
    /// Asynchronously reads all available messages for this subscription.
    /// </summary>
    /// <param name="cancellationToken">Token that cancels asynchronous enumeration.</param>
    /// <returns>An asynchronous sequence of subscribed messages.</returns>
    IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken = default);
}
