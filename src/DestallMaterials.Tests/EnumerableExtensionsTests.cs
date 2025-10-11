using DestallMaterials.WheelProtection.Extensions.Enumerables;
using DestallMaterials.WheelProtection.Extensions.Tasks;

namespace DestallMaterials.Tests;

public class EnumerableExtensionsTests
{
    readonly List<string> _output = [];

    [SetUp]
    public void Setup()
    {
    }


    [Test]
    public void TestSplit()
    {
        var result = Enumerable.Range(0, 100).Split(10).Select(e => e.ToArray());
    }

    [Test]

    public async Task AwaitErroredTuple()
    {
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            var (a, b) = await (Task.FromResult(100), Task.Run(() => { throw new TaskCanceledException(); return 15; }));

            var c = a + b;
        });
    }

    [Test]
    public async Task AwaitErroredTasksList()
    {
        var tasks = Enumerable.Range(0, 10).WhenEachAsync(async i =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(i * 100));
            throw new TaskCanceledException();
            return 0;
        });

        Assert.ThrowsAsync<AggregateException>(() => tasks.ToListAsync());
    }

    [Test]
    public async Task WhenAll_Behaviour()
    {
        var results = new List<int>();

        await Enumerable
            .Range(0, 2).Select(i => i == 0 ? Task.Delay(100).Then(() => results.Add(1)) : Task.CompletedTask.Then(() => results.Add(0)))
            .WhenAll();

        Assert.AreEqual(results[0], 0);
        Assert.AreEqual(results[1], 1);
    }

    [Test]
    public void SplitBy()
    {
        var a = Enumerable.Range(1, 10).SplitBy(n => n % 3 == 0).ToArray();

    }
}