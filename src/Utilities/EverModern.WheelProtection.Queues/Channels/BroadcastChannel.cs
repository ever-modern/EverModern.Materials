using System.Collections.Concurrent;
using System.Threading.Channels;

namespace EverModern.Threading.Channels;

/// <summary>
/// Represents a fan-out channel where written outgoing messages
/// are distributed to subscriptions.
/// </summary>
/// <typeparam name="TMessage">
/// The message type accepted by the writer and observed by subscribers.
/// </typeparam>
public sealed class BroadcastChannel<TMessage> : IBroadcastChannel<TMessage, TMessage>, IDisposable
{
    private readonly Channel<TMessage> _outgoingChannel;

    private readonly ConcurrentDictionary<Guid, Channel<TMessage>> _subscribers = new();

    private readonly CancellationTokenSource _cts = new();

    private readonly Task _broadcastTask;

    private readonly BoundedChannelOptions? _subscriberBoundedOptions;

    private readonly UnboundedChannelOptions? _subscriberUnboundedOptions;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BroadcastChannel{TMessage}"/> class using bounded
    /// outgoing and subscriber channels.
    /// </summary>
    /// <param name="capacity">
    /// The maximum capacity of the outgoing channel and each subscriber
    /// channel.
    /// </param>
    /// <remarks>
    /// Both channel types use
    /// <see cref="BoundedChannelFullMode.DropOldest"/> by default.
    /// When capacity is exceeded, the oldest messages are discarded.
    /// </remarks>
    public BroadcastChannel(int capacity = 1000)
        : this(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            },
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true,
            }
        ) { }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BroadcastChannel{TMessage}"/> class using bounded
    /// outgoing and subscriber channels.
    /// </summary>
    /// <param name="outgoingOptions">
    /// Configuration options for the outgoing channel.
    /// </param>
    /// <param name="subscriberOptions">
    /// Configuration options for subscriber channels.
    /// </param>
    public BroadcastChannel(
        BoundedChannelOptions outgoingOptions,
        BoundedChannelOptions subscriberOptions
    )
    {
        _outgoingChannel = Channel.CreateBounded<TMessage>(outgoingOptions);

        _subscriberBoundedOptions = subscriberOptions;

        _broadcastTask = Task.Run(BroadcastLoop);
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BroadcastChannel{TMessage}"/> class using a bounded
    /// outgoing channel and unbounded subscriber channels.
    /// </summary>
    /// <param name="outgoingOptions">
    /// Configuration options for the outgoing channel.
    /// </param>
    /// <param name="subscriberOptions">
    /// Configuration options for subscriber channels.
    /// </param>
    public BroadcastChannel(
        BoundedChannelOptions outgoingOptions,
        UnboundedChannelOptions subscriberOptions
    )
    {
        _outgoingChannel = Channel.CreateBounded<TMessage>(outgoingOptions);

        _subscriberUnboundedOptions = subscriberOptions;

        _broadcastTask = Task.Run(BroadcastLoop);
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BroadcastChannel{TMessage}"/> class using an
    /// unbounded outgoing channel and bounded subscriber channels.
    /// </summary>
    /// <param name="outgoingOptions">
    /// Configuration options for the outgoing channel.
    /// </param>
    /// <param name="subscriberOptions">
    /// Configuration options for subscriber channels.
    /// </param>
    public BroadcastChannel(
        UnboundedChannelOptions outgoingOptions,
        BoundedChannelOptions subscriberOptions
    )
    {
        _outgoingChannel = Channel.CreateUnbounded<TMessage>(outgoingOptions);

        _subscriberBoundedOptions = subscriberOptions;

        _broadcastTask = Task.Run(BroadcastLoop);
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="BroadcastChannel{TMessage}"/> class using unbounded
    /// outgoing and subscriber channels.
    /// </summary>
    /// <param name="outgoingOptions">
    /// Configuration options for the outgoing channel.
    /// </param>
    /// <param name="subscriberOptions">
    /// Configuration options for subscriber channels.
    /// </param>
    public BroadcastChannel(
        UnboundedChannelOptions outgoingOptions,
        UnboundedChannelOptions subscriberOptions
    )
    {
        _outgoingChannel = Channel.CreateUnbounded<TMessage>(outgoingOptions);

        _subscriberUnboundedOptions = subscriberOptions;

        _broadcastTask = Task.Run(BroadcastLoop);
    }

    /// <summary>
    /// Gets the writer used to publish messages to the broadcast
    /// channel.
    /// </summary>
    public ChannelWriter<TMessage> Writer => _outgoingChannel.Writer;

    /// <summary>
    /// Writes a message to the broadcast channel.
    /// </summary>
    /// <param name="message">
    /// The message to broadcast to subscribers.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the write operation.
    /// </param>
    public async ValueTask WriteAsync(
        TMessage message,
        CancellationToken cancellationToken = default
    )
    {
        await _outgoingChannel.Writer.WriteAsync(message, cancellationToken);
    }

    /// <summary>
    /// Creates a new subscription that receives messages matching
    /// the specified filter.
    /// </summary>
    /// <param name="filter">
    /// Predicate used to decide whether a message is yielded for this
    /// subscription.
    /// </param>
    /// <returns>
    /// A subscription that can asynchronously read matching messages.
    /// </returns>
    public IChannelSubscription<TMessage> Subscribe(Func<TMessage, bool> filter)
    {
        var id = Guid.NewGuid();

        Channel<TMessage> channel = _subscriberBoundedOptions is not null
            ? Channel.CreateBounded<TMessage>(_subscriberBoundedOptions)
            : Channel.CreateUnbounded<TMessage>(_subscriberUnboundedOptions!);

        if (!_subscribers.TryAdd(id, channel))
        {
            throw new InvalidOperationException("Failed to add subscriber.");
        }

        return new ChannelSubscription<TMessage>(
            channel.Reader,
            (_) => RemoveSubscriber(id),
            filter
        );
    }

    private async Task BroadcastLoop()
    {
        try
        {
            await foreach (var message in _outgoingChannel.Reader.ReadAllAsync(_cts.Token))
            {
                foreach (var subscriber in _subscribers.Values)
                {
                    try
                    {
                        await subscriber.Writer.WriteAsync(message, _cts.Token);
                    }
                    catch (ChannelClosedException) { }
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            foreach (var subscriber in _subscribers.Values)
            {
                subscriber.Writer.TryComplete();
            }

            _subscribers.Clear();
        }
    }

    private void RemoveSubscriber(Guid id)
    {
        if (_subscribers.TryRemove(id, out var channel))
        {
            channel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// Releases resources used by the broadcast channel and completes
    /// all subscriber channels.
    /// </summary>
    public void Dispose()
    {
        _cts.Cancel();

        _outgoingChannel.Writer.TryComplete();

        try
        {
            _broadcastTask.Wait();
        }
        catch (AggregateException ex)
            when (ex.InnerExceptions.All(x => x is OperationCanceledException)) { }

        _cts.Dispose();
    }
}
