using EverModern.WheelProtection.Extensions;
using EverModern.WheelProtection.Extensions.Tasks;

namespace EverModern.Tests.XUnit;

public class ExtensionsTests
{
    [Fact]
    public async Task AwaitTuple()
    {
        var firstTask = Task.FromResult(1);
        var secondTask = Task.FromResult(2);
        var thirdTask = Task.FromResult(3);
        var fourthTask = Task.FromResult(4);

        var (first, second, third, fourth) = await (firstTask, secondTask, thirdTask, fourthTask);

        Assert.Equal(10, first + second + third + fourth);
    }

    [Fact]
    public void GetEnumeratorOfTuple()
    {
        int i = 0;
        foreach (var item in (1.0, 12.5, 32.0))
        {
            i++;
        }
        Assert.Equal(3, i);
    }

    [Fact]
    public void DeconstructList()
    {
        var list = (1, 2.0, 3, 4.0, 5, 6.0).ToDictionary();
    }

    [Fact]
    public async Task SelectTask_AwaitThem()
    {
        var (n1, n2, n3, n4, n5) = await (1, 2, 3, 4, 5).Select(async n =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(n));
            return n;
        });
    }

    [Fact]
    public void DeconstructionTesting()
    {
        var arr = (1, 2, 3, 4, 5, 6).ToArray();
    }
}
