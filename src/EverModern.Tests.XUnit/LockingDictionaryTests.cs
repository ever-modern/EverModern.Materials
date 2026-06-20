namespace EverModern.Tests.XUnit;

using EverModern.Threading.Locks;

public class LockingDictionaryTests
{
    [Fact]
    public void Acquire_CreatesEntryWithKeyAndValue()
    {
        using var dict = new LockingDictionary<string, int>();

        using var entry = dict.Acquire("a", k => 42);

        Assert.NotNull(entry);
        Assert.Equal("a", entry.Key);
        Assert.Equal(42, entry.Value);
    }

    [Fact]
    public void Acquire_SameKey_ReturnsCachedValue()
    {
        using var dict = new LockingDictionary<string, int>();
        var factoryCalls = 0;

        using (var entry1 = dict.Acquire("x", k => { factoryCalls++; return 1; }))
        {
            Assert.Equal(1, entry1.Value);
        }

        using (var entry2 = dict.Acquire("x", k => { factoryCalls++; return 999; }))
        {
            Assert.Equal(1, entry2.Value); // cached, not 999
        }

        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public void Acquire_DifferentKeys_Independent()
    {
        using var dict = new LockingDictionary<string, int>();

        using var entryA = dict.Acquire("a", k => 10);
        using var entryB = dict.Acquire("b", k => 20);

        Assert.Equal(10, entryA.Value);
        Assert.Equal(20, entryB.Value);
    }

    [Fact]
    public void Acquire_SameKey_SerializesAccess()
    {
        using var dict = new LockingDictionary<string, int>();

        var entry1 = dict.Acquire("k", k => 1);

        var blocked = false;
        var thread = new Thread(() =>
        {
            blocked = true;
            using var entry2 = dict.Acquire("k", k => 2);
            blocked = false;
        });
        thread.Start();

        Thread.Sleep(100);
        Assert.True(blocked);

        entry1.Dispose();
        thread.Join(1000);
        Assert.False(blocked);
    }

    [Fact]
    public void Entry_ValueSetter_UpdatesStore()
    {
        using var dict = new LockingDictionary<string, int>();

        using var entry = dict.Acquire("k", k => 1);
        Assert.Equal(1, entry.Value);

        entry.Value = 100;
        Assert.Equal(100, entry.Value);

        // Verify the store was updated by acquiring again
        using var entry2 = dict.Acquire("k", k => 999);
        Assert.Equal(100, entry2.Value);
    }

    [Fact]
    public void Entry_Remove_RemovesFromStore()
    {
        using var dict = new LockingDictionary<string, int>();

        var entry = dict.Acquire("k", k => 42);
        entry.Remove();

        // After remove, a new acquire should create a fresh value
        using var entry2 = dict.Acquire("k", k => 99);
        Assert.Equal(99, entry2.Value);
    }

    [Fact]
    public void Entry_Dispose_DoesNotRemoveFromStore()
    {
        using var dict = new LockingDictionary<string, int>();

        var entry = dict.Acquire("k", k => 42);
        entry.Dispose();

        // After dispose (not remove), value still exists
        using var entry2 = dict.Acquire("k", k => 99);
        Assert.Equal(42, entry2.Value);
    }

    [Fact]
    public void Entry_Dispose_Twice_DoesNotThrow()
    {
        using var dict = new LockingDictionary<string, int>();

        var entry = dict.Acquire("k", k => 1);
        entry.Dispose();
        entry.Dispose();
    }

    [Fact]
    public void Entry_Remove_Twice_DoesNotThrow()
    {
        using var dict = new LockingDictionary<string, int>();

        var entry = dict.Acquire("k", k => 1);
        entry.Remove();
        entry.Remove();
    }

    [Fact]
    public void Entry_AccessAfterDispose_Throws()
    {
        using var dict = new LockingDictionary<string, int>();

        var entry = dict.Acquire("k", k => 42);
        entry.Dispose();

        Assert.Throws<ObjectDisposedException>(() => entry.Value);
        Assert.Throws<ObjectDisposedException>(() => { entry.Value = 99; });
    }

    [Fact]
    public void Acquire_WithCustomComparer()
    {
        using var dict = new LockingDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        using var entry1 = dict.Acquire("KEY", k => 10);

        // Same key with different case should serialize
        var blocked = false;
        var thread = new Thread(() =>
        {
            blocked = true;
            using var entry2 = dict.Acquire("key", k => 20);
            blocked = false;
        });
        thread.Start();

        Thread.Sleep(100);
        Assert.True(blocked);

        entry1.Dispose();
        thread.Join(1000);
        Assert.False(blocked);

        // Value was cached by the first acquisition
        using var entry3 = dict.Acquire("key", k => 30);
        Assert.Equal(10, entry3.Value);
    }

    [Fact]
    public void MultipleThreads_SameKey_Serializes()
    {
        using var dict = new LockingDictionary<int, int>();
        var concurrentCount = 0;

        const int threadCount = 20;
        var tasks = new Task[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                using var entry = dict.Acquire(42, k => k * k);
                var current = Interlocked.Increment(ref concurrentCount);

                Assert.Equal(1, current);

                Thread.Sleep(10);

                Interlocked.Decrement(ref concurrentCount);
            });
        }

        foreach (var t in tasks) t.Wait();
    }

    [Fact]
    public void MultipleThreads_DifferentKeys_NoContention()
    {
        using var dict = new LockingDictionary<int, int>();

        var tasks = new Task[20];

        for (int i = 0; i < 20; i++)
        {
            var key = i;
            tasks[i] = Task.Run(() =>
            {
                using var entry = dict.Acquire(key, k => k * 2);
                Assert.Equal(key * 2, entry.Value);
            });
        }

        foreach (var t in tasks) t.Wait();
    }

    [Fact]
    public void Concurrent_AcquireAndUpdate_SameKey()
    {
        using var dict = new LockingDictionary<int, int>();

        // Pre-populate
        using (var seed = dict.Acquire(1, k => 0)) { }

        var tasks = Enumerable.Range(0, 10).Select(i =>
            Task.Run(() =>
            {
                using var entry = dict.Acquire(1, k => 0);
                entry.Value = entry.Value + 1;
            })
        ).ToArray();

        foreach (var t in tasks) t.Wait();

        using var final = dict.Acquire(1, k => 0);
        Assert.Equal(10, final.Value);
    }
}
