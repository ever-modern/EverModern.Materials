using EverModern.Threading.Locks;

namespace EverModern.Tests.XUnit;

public class AsyncKeyLockerTests
{
    [Fact]
    public async Task LockAsync_AcquireAndRelease()
    {
        using var locker = new AsyncKeyLocker<string>();
        using var scope = await locker.LockAsync("key1");

        Assert.NotNull(scope);
        Assert.IsType<LockedScope>(scope);
    }

    [Fact]
    public async Task DifferentKeys_Independent()
    {
        using var locker = new AsyncKeyLocker<string>();

        using var scope1 = await locker.LockAsync("key1");
        using var scope2 = await locker.LockAsync("key2");

        Assert.NotNull(scope1);
        Assert.NotNull(scope2);
    }

    [Fact]
    public async Task SameKey_Serializes()
    {
        using var locker = new AsyncKeyLocker<string>();

        var scope1 = await locker.LockAsync("key1");

        var secondTask = locker.LockAsync("key1");

        Assert.False(secondTask.IsCompleted);

        ((IDisposable)scope1).Dispose();

        using var scope2 = await secondTask;
        Assert.NotNull(scope2);
    }

    [Fact]
    public async Task Cancellation_AllowsNextWaiterThrough()
    {
        using var locker = new AsyncKeyLocker<string>();

        var scope1 = await locker.LockAsync("key1");

        using var cts = new CancellationTokenSource();

        var cancelledTask = locker.LockAsync("key1", cts.Token);
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await cancelledTask);

        var thirdTask = locker.LockAsync("key1");
        Assert.False(thirdTask.IsCompleted);

        ((IDisposable)scope1).Dispose();

        using var scope3 = await thirdTask;
        Assert.NotNull(scope3);
    }

    [Fact]
    public async Task Dispose_ThrowsObjectDisposed()
    {
        var locker = new AsyncKeyLocker<string>();
        locker.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await locker.LockAsync("key1"));
    }

    [Fact]
    public async Task MultipleThreads_SameKey()
    {
        using var locker = new AsyncKeyLocker<int>();
        var concurrentCount = 0;

        const int threadCount = 20;
        var tasks = new Task[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                using var scope = await locker.LockAsync(42);
                var current = Interlocked.Increment(ref concurrentCount);

                Assert.Equal(1, current);

                await Task.Delay(10);

                Interlocked.Decrement(ref concurrentCount);
            });
        }

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task MultipleThreads_DifferentKeys()
    {
        using var locker = new AsyncKeyLocker<int>();

        var tasks = new Task[20];

        for (int i = 0; i < 20; i++)
        {
            var key = i;
            tasks[i] = Task.Run(async () =>
            {
                using var scope = await locker.LockAsync(key);
                await Task.Delay(10);
            });
        }

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task Dispose_WithActiveLocks_NoSemaphoreLeak()
    {
        var locker = new AsyncKeyLocker<string>();

        var scope1 = await locker.LockAsync("key1");
        var scope2 = await locker.LockAsync("key2");

        locker.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await locker.LockAsync("key3"));

        ((IDisposable)scope1).Dispose();
        ((IDisposable)scope2).Dispose();
    }

    [Fact]
    public async Task RefCounting_Correct()
    {
        using var locker = new AsyncKeyLocker<string>();

        using (var scope1 = await locker.LockAsync("key")) { }
        using (var scope2 = await locker.LockAsync("key")) { }
        using (var scope3 = await locker.LockAsync("key")) { }
    }

    [Fact]
    public async Task WithCustomComparer()
    {
        using var locker = new AsyncKeyLocker<string>(StringComparer.OrdinalIgnoreCase);

        var scope1 = await locker.LockAsync("KEY");

        var secondTask = locker.LockAsync("key");
        Assert.False(secondTask.IsCompleted);

        ((IDisposable)scope1).Dispose();

        using var scope2 = await secondTask;
    }

    [Fact]
    public async Task ManyKeys_NoDeadlock()
    {
        using var locker = new AsyncKeyLocker<int>();

        var tasks = Enumerable.Range(0, 50).Select(i =>
            Task.Run(async () =>
            {
                for (int j = 0; j < 10; j++)
                {
                    using var scope = await locker.LockAsync(i % 5);
                    await Task.Delay(5);
                }
            })
        ).ToArray();

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task Dispose_ThenReleaseActiveLocks_DoesNotThrow()
    {
        var locker = new AsyncKeyLocker<string>();

        var scope1 = await locker.LockAsync("a");
        var scope2 = await locker.LockAsync("b");
        var scope3 = await locker.LockAsync("c");

        locker.Dispose();

        ((IDisposable)scope1).Dispose();
        ((IDisposable)scope2).Dispose();
        ((IDisposable)scope3).Dispose();
    }
}
