using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using DestallMaterials.WheelProtection.Extensions.Tasks;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[RPlotExporter]
public class DifferentInvocations
{
    [Params(1000, 10000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public (int, int, int) Using_Task_WhenAll()
        => DestallMaterials.WheelProtection.Extensions.Tasks.TaskExtensions.GetAwaiter((RunBigTask(), RunBigTask(), RunBigTask()))
            .GetResult();

    [Benchmark]
    public (int, int, int, int, int) Using_Task_WhenAll_5()
    => DestallMaterials.WheelProtection.Extensions.Tasks.TaskExtensions.GetAwaiter((RunBigTask(), RunBigTask(), RunBigTask(), RunBigTask(), RunBigTask()))
        .GetResult();

    [Benchmark]
    public (int, int, int, int, int, int, int) Using_Task_WhenAll_7()
        => DestallMaterials.WheelProtection.Extensions.Tasks.TaskExtensions.GetAwaiter_TaskWhenAll((RunBigTask(), RunBigTask(), RunBigTask(), RunBigTask(), RunBigTask(), RunBigTask(), RunBigTask()))
            .GetResult();
    Task<int> RunBigTask() => Task.Run(() =>
    {
        var n = N;
        return FindNthPrime(n);
    });

    static int FindNthPrime(int n)
    {
        int count = 0;
        int num = 2;

        while (true)
        {
            if (IsPrime(num))
            {
                count++;
                if (count == n)
                {
                    return num;
                }
            }
            num++;
        }
    }

    static bool IsPrime(int num)
    {
        if (num <= 1) return false;
        if (num == 2) return true;
        if (num % 2 == 0) return false;

        var boundary = (int)Math.Floor(Math.Sqrt(num));

        for (int i = 3; i <= boundary; i += 2)
        {
            if (num % i == 0) return false;
        }

        return true;
    }
}