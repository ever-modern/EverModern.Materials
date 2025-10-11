using DestallMaterials.WheelProtection.Caching;

namespace DestallMaterials.Tests;

public class CachingTests
{
    [Test]
    public async Task CacherDoesntDoUnnecessaryIterations()
    {
        var cachingSettings = new CachingSettings()
        {
            MaxSize = 100,
            Validity = TimeSpan.FromSeconds(3)
        };
        var cacher = new Cacher<int, Task<int>>(async n => 
        {
            Console.WriteLine($"Launched calculation of square {n}.");
            await Task.Delay(1000);
            return n * n;
        }, n => n, n => cachingSettings);

        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine(await cacher.Run(3));
        }
    }
}
