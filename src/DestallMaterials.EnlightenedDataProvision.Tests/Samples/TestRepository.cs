using DestallMaterials.WheelProtection.Extensions.Tasks;
using DestallMaterials.WheelProtection.Queues;

namespace DestallMaterials.EnlightenedDataProvision.Tests.Samples;

class TestDbContextRecycler : Recycler<TestDbContext>
{
    public TestDbContextRecycler(int maxPoolSize) : base(maxPoolSize)
    {
    }

    protected override void Discard(TestDbContext item)
        => item.Dispose();

    protected override bool IsWell(TestDbContext item)
        => true;

    protected override bool TryCreateNew(out TestDbContext item)
    {
        item = TestDbContext.ForFileName("recycled.db");
        return true;
    }
}

public class TestRepository : EnlightenedRepository<long, TestBaseEntity, TestDbContext>
{
    public static PooledDbContextSource<TestDbContext> CreateDbContextSource(int poolSize)
    {
        var recycler = new TestDbContextRecycler(poolSize);
        return async (ct) => await recycler
            .Another(ct)
            .AsTask()
            .WithinDeadline(TimeSpan.FromSeconds(10));
    }

    static TestDbContext GetDbContextAtOnce(PooledDbContextSource<TestDbContext>factory)
    {
        using var a = factory(default).Result;
        var dbContext = a.Item;
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    public TestRepository(PooledDbContextSource<TestDbContext> dbContextFactory)
        : base(dbContextFactory, GetDbContextAtOnce(dbContextFactory))
    {
    }
}
