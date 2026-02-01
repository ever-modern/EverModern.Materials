using System.Runtime.CompilerServices;

namespace EverModern.WheelProtection.Extensions.Time;

public static class TimeExtensions
{
    public static TaskAwaiter GetAwaiter(this TimeSpan time)
       => Task.Delay(time).GetAwaiter();

    public static TaskAwaiter GetAwaiter(this DateTime dateTime)
        => Task.Delay(dateTime - DateTime.UtcNow).GetAwaiter();

    public static DateTime RoundUpTo(this DateTime dateTime, TimeSpan measure)
    {
        var ticks = (dateTime - default(DateTime)).Ticks;
        var intResult = ticks / measure.Ticks;
        var result = intResult * measure.Ticks == ticks ? intResult * measure.Ticks : intResult + 1;
        return default(DateTime) + TimeSpan.FromTicks(result);
    }

    public static DateTime RoundDownTo(this DateTime dateTime, TimeSpan measure)
    {
        var ticks = (dateTime - default(DateTime)).Ticks;
        var intResult = ticks / measure.Ticks;
        var result = intResult * measure.Ticks == ticks ? intResult * measure.Ticks : intResult - 1;
        return default(DateTime) + TimeSpan.FromTicks(result);
    }

    public static DateTime RoundTo(this DateTime dateTime, TimeSpan measure)
    {
        var ticks = (dateTime - default(DateTime)).Ticks;
        var intResult = (int)Math.Round((decimal)ticks / measure.Ticks);
        var result = intResult * measure.Ticks == ticks ? intResult * measure.Ticks : intResult - 1;
        return default(DateTime) + TimeSpan.FromTicks(result);
    }
}
