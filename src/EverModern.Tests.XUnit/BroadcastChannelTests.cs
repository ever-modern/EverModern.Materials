using EverModern.Threading.Channels;

namespace EverModern.Tests.XUnit;

public class BroadcastChannelTests
{
    static async Task<T> ReadSingleAsync<T>(
        IChannelSubscription<T> subscription,
        CancellationToken cancellationToken = default)
    {
        await foreach (var message in subscription.ReadAllAsync(cancellationToken))
        {
            return message;
        }

        throw new InvalidOperationException("Subscription completed before yielding a message.");
    }

    [Fact]
    public async Task BroadcastsMessageToAllSubscribers()
    {
        using var channel = new BroadcastChannel<int>();
        using var subscription1 = channel.Subscribe(_ => true);
        using var subscription2 = channel.Subscribe(_ => true);

        await channel.Writer.WriteAsync(42);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var first = await ReadSingleAsync(subscription1, cts.Token);
        var second = await ReadSingleAsync(subscription2, cts.Token);

        Assert.Equal(42, first);
        Assert.Equal(42, second);
    }

    [Fact]
    public async Task SubscriptionFilter_IsApplied()
    {
        using var channel = new BroadcastChannel<int>();
        using var subscription = channel.Subscribe(message => message % 2 == 0);

        await channel.Writer.WriteAsync(1);
        await channel.Writer.WriteAsync(2);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var received = await ReadSingleAsync(subscription, cts.Token);

        Assert.Equal(2, received);
    }

    [Fact]
    public async Task DisposedSubscription_DoesNotReceiveFurtherMessages()
    {
        using var channel = new BroadcastChannel<int>();
        var subscription = channel.Subscribe(_ => true);

        await channel.Writer.WriteAsync(10);

        using var firstReadCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var first = await ReadSingleAsync(subscription, firstReadCts.Token);
        Assert.Equal(10, first);

        subscription.Dispose();

        await channel.Writer.WriteAsync(11);

        using var secondReadCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await ReadSingleAsync(subscription, secondReadCts.Token);
        });
    }
}
