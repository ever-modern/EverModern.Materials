using System.Runtime.CompilerServices;

namespace DestallMaterials.WheelProtection.Extensions.Time
{
    public static class TimeExtensions
    {
        public static TaskAwaiter GetAwaiter(this TimeSpan time)
           => Task.Delay(time).GetAwaiter();

        public static TaskAwaiter GetAwaiter(this DateTime dateTime)
            => Task.Delay(dateTime - DateTime.UtcNow).GetAwaiter();

        public static DateTime TrimUp(this DateTime dateTime, TimeSpan measure)
        {
            var ticks = (dateTime - default(DateTime)).Ticks;
            var intResult = ticks / measure.Ticks;
            var result = intResult * measure.Ticks == ticks ? intResult * measure.Ticks : intResult + 1;
            return default(DateTime) + TimeSpan.FromTicks(result);
        }

        public static DateTime TrimDown(this DateTime dateTime, TimeSpan measure)
        {
            var ticks = (dateTime - default(DateTime)).Ticks;
            var intResult = ticks / measure.Ticks;
            var result = intResult * measure.Ticks == ticks ? intResult * measure.Ticks : intResult - 1;
            return default(DateTime) + TimeSpan.FromTicks(result);
        }
    }
}
