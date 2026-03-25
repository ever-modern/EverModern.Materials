using EverModern.WheelProtection.DataStructures.Time;

namespace EverModern.Tests.XUnit;

public class DateTimeRangeTesting
{
    DateTime CreateDate(int seconds) => new DateTime() + TimeSpan.FromSeconds(seconds);

    [Fact]
    public void Split()
    {
        var start = CreateDate(10);
        var end = CreateDate(51);
        var dateTimeRange = new DateTimeRange(start, end);

        var splits = dateTimeRange.Split(TimeSpan.FromSeconds(10));

        Assert.Equal(5, splits.Length);
        for (int i = 0; i < splits.Length - 1; i++)
        {
            var item = splits[i];
            Assert.Equal(TimeSpan.FromSeconds(10), item.Duration);
            Assert.Equal(CreateDate(10 + i * 10), item.Start);
            Assert.Equal(CreateDate(20 + i * 10), item.End);
        }

        Assert.Equal(CreateDate(50), splits[4].Start);
        Assert.Equal(CreateDate(51), splits[4].End);
    }

    [Fact]
    public void Merge()
    {
        const int n = 200;
        var dates = Enumerable
            .Range(0, n)
            .Select(i => new DateTimeRange(CreateDate(i), CreateDate(i + 1)))
            .ToArray();

        var merged = DateTimeRange.Merge(dates);

        Assert.Equal(CreateDate(0), merged.Start);
        Assert.Equal(CreateDate(n), merged.End);
    }

    [Fact]
    public void Intersect()
    {
        var date1 = CreateDate(0);
        var date2 = CreateDate(10);

        var date3 = CreateDate(7);
        var date4 = CreateDate(12);

        var range1 = new DateTimeRange(date1, date2);
        var range2 = new DateTimeRange(date3, date4);

        var intersects1 = range1.Intersects(range2, out var intersection1);

        Assert.True(intersects1);

        Assert.Equal(intersection1.Start, date3);
        Assert.Equal(intersection1.End, date2);

        var intersects2 = range2.Intersects(range1, out var intersection2);

        Assert.True(intersects2);
        Assert.Equal(intersection1, intersection2);

        var intersectsItself = range1.Intersects(range1, out var intersectionWithItself);

        Assert.Equal(intersectionWithItself, range1);
    }

    [Fact]
    public void Intersect_False()
    {
        var date1 = CreateDate(0);
        var date2 = CreateDate(5);

        var date3 = CreateDate(7);
        var date4 = CreateDate(12);

        var range1 = new DateTimeRange(date1, date2);
        var range2 = new DateTimeRange(date3, date4);

        var intersects1 = range1.Intersects(range2, out var intersection1);

        Assert.False(intersects1);

        Assert.Equal(intersection1.Start, default);
        Assert.Equal(intersection1.End, default);

        var intersects2 = range2.Intersects(range1, out var intersection2);

        Assert.False(intersects2);
        Assert.Equal(intersection1, intersection2);
    }
}
