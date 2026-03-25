using EverModern.Chronos;
using System.Diagnostics;

namespace EverModern.Tests.XUnit;



public class ChronosTesting
{
    (Func<DateTimeOffset> Getter, Action<DateTimeOffset> Setter) GetManuallySettableTimeSource(DateTimeOffset initialValue = default)
    {
        var now = initialValue;
        return (() => now, (newNow) => now = newNow);
    }

    [Theory]
    [InlineData(2000, 0.5, 2, 4, 2002)]
    [InlineData(2000, 1, 5, 5, 2005)]
    [InlineData(2000, 1000, 2, 2, 2000 * 2)]
    public async Task AwaitMilliseconds_ShouldMatch(
        int startingTimeMilliseconds,
        decimal timeSpeed,
        int passMilliseconds,
        int skipRealTimeMilliseconds,
        int expectedMillisecondsAfterWaiting)
    {
        var (getNow, setNow) = GetManuallySettableTimeSource();
        var initTime = getNow() + TimeSpan.FromMilliseconds(startingTimeMilliseconds);
        var chronos = new Chronos.ManualChronos(
            initialTime: initTime,
            relativeSpeed: timeSpeed,
            realTimeSource: getNow);

        var currentMilliseconds = () => (chronos.Now - default(DateTimeOffset)).TotalMilliseconds;

        TimeSpan passedTime = TimeSpan.FromMilliseconds(passMilliseconds);

        DateTimeOffset newTime = getNow().AddMilliseconds(skipRealTimeMilliseconds);

        setNow(newTime);

        Assert.Equal(expectedMillisecondsAfterWaiting, currentMilliseconds());
    }

    [Fact]
    public void Create_MustHaveRightTimeAtStart()
    {
        var (getNow, setNow) = GetManuallySettableTimeSource();

        var chronos = new Chronos.ManualChronos(default(DateTimeOffset) + TimeSpan.FromMilliseconds(2000), 0.5m, getNow);

        Assert.Equal(chronos.Now.DateTime, new DateTime() + TimeSpan.FromMilliseconds(2000));
    }

    [Fact]
    public async Task Await_ChronosTimeMustPassQuicker_ButNotTooQuickly()
    {
        var startTime = DateTimeOffset.Now;

        int speed = 1000;
        long millisecondsToWait = 2000;

        var chronos = new Chronos.ManualChronos(startTime, speed);

        await Task.Delay(TimeSpan.FromMilliseconds(millisecondsToWait));

        var now = chronos.Now;
        var chronosTimePassed = now - startTime;
        var expectedMinPassedTime = TimeSpan.FromMilliseconds(speed * millisecondsToWait);
        var absoluteDifference = chronosTimePassed - expectedMinPassedTime;
        var relativeDifference = (decimal)absoluteDifference.Ticks / (decimal)expectedMinPassedTime.Ticks;


        Assert.True(absoluteDifference.Ticks > 0);
        Assert.True(relativeDifference < 0.008m);
    }

    [Fact]
    public void MoveTimeForward_MustMoveExactlyToParameter()
    {
        var (getNow, setNow) = GetManuallySettableTimeSource();

        var chronos = new ManualChronos();

        var now1 = chronos.Now;

        var timeDifference = TimeSpan.FromSeconds(1);

        chronos.MoveTime(timeDifference);

        var now2 = chronos.Now;

        Assert.Equal(timeDifference, now2 - now1);
    }

    [Fact]
    public async Task Measure_Find_NonMatteringTaskDelay()
    {
        var results = new List<int>();
        for (int i = 0; i < 50; i++)
        {
            var stopwatch = Stopwatch.StartNew();

            await Task.Delay(1);

            results.Add((int)stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    [Fact]
    public void SetTime()
    {
        var t0 = DateTimeOffset.Now.AddDays(-20);
        var t1 = DateTimeOffset.Now.AddDays(-200);
        var chronos = new ManualChronos(initialTime: t0, 0);

        var chronosTime0 = chronos.Now;

        chronos.SetTime(t1);

        var chronosTime1 = chronos.Now;

        Assert.Equal(t0, chronosTime0);
        Assert.Equal(t1, chronosTime1);
    }
}
