using EverModern.Threading.Locks;

namespace EverModern.Tests.XUnit;

public class KeyLockerTests
{
    [Fact]
    public void Lock_AcquireAndRelease()
    {
        using var locker = new KeyLocker<string>();
        using var scope = locker.Lock("key1");

        Assert.NotNull(scope);
        Assert.IsType<LockedScope>(scope);
    }

    [Fact]
    public void DifferentKeys_Independent()
    {
        using var locker = new KeyLocker<string>();

        using var scope1 = locker.Lock("key1");
        using var scope2 = locker.Lock("key2");

        Assert.NotNull(scope1);
        Assert.NotNull(scope2);
    }

    [Fact]
    public void SameKey_Serializes()
    {
        using var locker = new KeyLocker<string>();

        var scope1 = locker.Lock("key1");

        var blocked = false;
        var thread = new Thread(() =>
        {
            blocked = true;
            using var scope2 = locker.Lock("key1");
            blocked = false;
        });
        thread.Start();

        Thread.Sleep(100);
        Assert.True(blocked);

        ((IDisposable)scope1).Dispose();
        thread.Join(1000);

        Assert.False(blocked);
    }

    [Fact]
    public void Dispose_ThrowsObjectDisposed()
    {
        var locker = new KeyLocker<string>();
        locker.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () => locker.Lock("key1"));
    }

    [Fact]
    public void MultipleThreads_SameKey()
    {
        using var locker = new KeyLocker<int>();
        var concurrentCount = 0;

        const int threadCount = 20;
        var threads = new Thread[threadCount];
        var exceptions = new List<Exception>();

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                try
                {
                    using var scope = locker.Lock(42);
                    var current = Interlocked.Increment(ref concurrentCount);

                    Assert.Equal(1, current);

                    Thread.Sleep(10);

                    Interlocked.Decrement(ref concurrentCount);
                }
                catch (Exception ex)
                {
                    lock (exceptions) { exceptions.Add(ex); }
                }
            });
        }

        foreach (var t in threads) t.Start();
        foreach (var t in threads) t.Join(5000);

        Assert.Empty(exceptions);
    }

    [Fact]
    public void WithCustomComparer()
    {
        using var locker = new KeyLocker<string>(StringComparer.OrdinalIgnoreCase);

        var scope1 = locker.Lock("KEY");

        var blocked = false;
        var thread = new Thread(() =>
        {
            blocked = true;
            using var scope2 = locker.Lock("key");
            blocked = false;
        });
        thread.Start();

        Thread.Sleep(100);
        Assert.True(blocked);

        ((IDisposable)scope1).Dispose();
        thread.Join(1000);

        Assert.False(blocked);
    }
}
