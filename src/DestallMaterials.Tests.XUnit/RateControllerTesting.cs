using DestallMaterials.Chronos;
using DestallMaterials.WheelProtection.Extensions.Tasks;
using DestallMaterials.WheelProtection.Queues;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DestallMaterials.Tests.XUnit;

public class RateControllerTesting
{
    [Fact]
    public async Task SimpleExample()
    {
        CancellationToken cancellationToken = CancellationToken.None;
        CallConstraint[] constraints =
        [
            new(TimeSpan.FromSeconds(1), 1),
        ];

        var nowProvider = new ManualChronos(
                relativeSpeed: 0,
                initialTime: new DateTime(2000, 1, 1));

        var rateController = new RateController(constraints, nowProvider);

        var waiting = rateController.WhenAllowed(cancellationToken).AsTask();

        Assert.True(waiting.IsCompleted);

        waiting = rateController.WhenAllowed(cancellationToken).AsTask();

        Assert.False(waiting.IsCompleted);

        nowProvider.MoveTime(TimeSpan.FromSeconds(0.5));

        Assert.False(waiting.IsCompleted);

        nowProvider.MoveTime(TimeSpan.FromSeconds(0.5));

        await waiting.WithinDeadline(TimeSpan.FromMilliseconds(1));

        Assert.True(waiting.IsCompleted);
    }

    [Fact]
    public async Task HarderExample()
    {
        CancellationToken cancellationToken = CancellationToken.None;
        CallConstraint[] constraints =
        [
            new(TimeSpan.FromSeconds(1), 1),
            new(TimeSpan.FromSeconds(5), 2),
        ];

        var nowProvider = new ManualChronos(
                relativeSpeed: 0,
                initialTime: new DateTime(2000, 1, 1));

        var rateController = new RateController(constraints, nowProvider);

        var moveTime = (double seconds) => nowProvider.MoveTime(TimeSpan.FromSeconds(seconds));

        Assert.True(rateController.WhenAllowed().IsCompletedSuccessfully);

        var second = rateController.WhenAllowed().AsTask();

        Assert.False(second.IsCompleted);

        moveTime(1);

        await second;

        var third = rateController.WhenAllowed().AsTask();

        Assert.False(third.IsCompleted);

        moveTime(2);

        Assert.False(third.IsCompleted);

        moveTime(2);

        Assert.True(third.IsCompleted);
    }
}
