using DestallMaterials.WheelProtection.Extensions.Arrays;
using DestallMaterials.WheelProtection.Extensions.Tasks;
using DestallMaterials.WheelProtection.Extensions.Time;
using DestallMaterials.WheelProtection.Linq;

namespace DestallMaterials.Tests;

public class ExtensionsTesting
{
    [Test]
    public async Task AwaitTuple()
    {
        var firstTask = Task.FromResult(1);
        var secondTask = Task.FromResult(2);
        var thirdTask = Task.FromResult(3);
        var fourthTask = Task.FromResult(4);

        var (first, second, third, fourth) = await (firstTask, secondTask, thirdTask, fourthTask);

        Assert.AreEqual(first + second + third + fourth, 10);
    }

    [Test]
    public void GetEnumeratorOfTuple()
    {
        int i = 0;
        foreach (var item in (1.0, 12.5, 32.0))
        {
            i++;
        }
        Assert.AreEqual(3, i);
    }

    [Test]
    public void DeconstructList()
    {
        var list = (1, 2.0, 3, 4.0, 5, 6.0).ToDictionary();
    }

    [Test]
    public async Task SelectTask_AwaitThem()
    {
        var (n1, n2, n3, n4, n5) = await (1, 2, 3, 4, 5).Select(async n =>
        {
            await TimeSpan.FromMilliseconds(n);
            return n;
        });
    }

    [Test]
    public void DeconstructionTesting()
    {
        var arr = (1, 2, 3, 4, 5, 6).ToArray();
        var (n1, n2, n3, n4, n5, n6) = arr;
    }
}
