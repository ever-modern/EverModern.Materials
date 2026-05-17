using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace EverModern.Threading.Channels;

/// <summary>
/// Default subscription implementation for <see cref="BroadcastChannel{TMessage}"/>.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class ChannelSubscription<T>(
    ChannelReader<T> reader,
    Action<ChannelSubscription<T>> onDisposed,
    Func<T, bool> filter
) : IChannelSubscription<T>
{
    /// <inheritdoc />
    public IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken = default) =>
        reader.ReadAllAsync(cancellationToken).Where(filter);

    /// <inheritdoc />
    public void Dispose() => onDisposed(this);
}
