using EverModern.WheelProtection.Extensions;
using EverModern.WheelProtection.Extensions.Enumerables;
using EverModern.WheelProtection.Extensions.Tasks;

namespace EverModern.Tests.XUnit;

public class EnumerableExtensionsTests
{
    readonly List<string> _output = [];

    [Fact]
    public void TestSplit()
    {
        var result = Enumerable.Range(0, 100).Split(10).Select(e => e.ToArray());
    }

    [Fact]
    public async Task AwaitErroredTuple()
    {
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            var (a, b) = await (Task.FromResult(100), Task.Run(() => { throw new TaskCanceledException(); return 15; }));

            var c = a + b;
        });
    }

    [Fact]
    public async Task AwaitErroredTasksList()
    {
        var tasks = Enumerable.Range(0, 10).WhenEachAsync(async i =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(i * 100));
            throw new TaskCanceledException();
            return 0;
        });

        await Assert.ThrowsAsync<AggregateException>(async () => await tasks.ToListAsync());
    }

    [Fact]
    public async Task WhenAll_Behaviour()
    {
        var results = new List<int>();

        await Enumerable
            .Range(0, 2).Select(i => i == 0 ? Task.Delay(100).Then(() => results.Add(1)) : Task.CompletedTask.Then(() => results.Add(0)))
            .WhenAll();

        Assert.Equal(0, results[0]);
        Assert.Equal(1, results[1]);
    }

    [Fact]
    public void SplitBy()
    {
        var a = Enumerable.Range(1, 10).SplitBy(n => n % 3 == 0).ToArray();
    }
}
