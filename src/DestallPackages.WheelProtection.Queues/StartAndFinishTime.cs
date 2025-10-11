using System;
using DateTime = System.DateTimeOffset;

namespace DestallMaterials.WheelProtection.Queues;

public class StartAndFinishTime
{
    public DateTime Start { get; set; }
    public DateTime Finish { get; set; }

    public StartAndFinishTime(DateTime start, DateTime finish)
    {
        Start = start;
        Finish = finish;
    }

    public StartAndFinishTime(DateTime start) : this(start, DateTime.MaxValue)
    {
    }

    public TimeSpan Duration => (Start > default(DateTime) && Finish > default(DateTime)) ? Finish - Start : default;

    public void Deconstruct(out DateTime start, out DateTime finish)
    {
        start = Start;
        finish = Finish;
    }
}