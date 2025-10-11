using DestallMaterials.EnlightenedDataProvision.Tests.Samples;
using DestallMaterials.WheelProtection.Extensions.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DestallMaterials.EnlightenedDataProvision.Tests;

public class RepositoryTests
{
    static readonly TestDbContext _dbContext;
    static readonly TestRepository _repository;
    static CancellationToken CancellationToken = CancellationToken.None;

    static readonly Func<TestRepository> _repositoryFactory;

    static RepositoryTests()
    {
        var dbContextSource = TestRepository.CreateDbContextSource(1);

        {
            using var dbContext = dbContextSource.Invoke(default).Result;

            _dbContext = dbContext;

            _dbContext.Database.EnsureDeleted();
            _dbContext.Database.EnsureCreated();

            _repositoryFactory = () => new(dbContextSource);
        }

        _repository = new(dbContextSource);
    }

    [Fact]
    public async Task Delete_Nothing_ButSuccessfully()
    {
        var set = _repository.Set<BigEntity>().Take(0);
        
        var deleted = await _repository.DeleteAsync(set, CancellationToken);

        await _repository.CancelChangesAsync(CancellationToken);
        
        Assert.Equal(deleted, 0);
    }

    [Fact]
    public async Task GrossCrudTest()
    {
        const int id = 11;

        const int ownedId_1 = 110;
        const int ownedId_2 = 1100;
        const int ownedId_3 = 11000;

        var repo = _repository;
        var set = repo.Set<BigEntity>();

        Assert.NotNull(set);

        //-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------

        var items = await repo
           .Set<BigEntity>()
           .Where(e => e.Id == 50)
           .Take(0)
           .ToArrayAsync();

        Assert.Equal(0, items.Length);

        //-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------

        var testEntity = new BigEntity()
        {
            Id = id,
            OwnedLines = [
                new()
                {
                    Id = ownedId_1
                },
                new()
                {
                    Id = ownedId_2
                },
                new()
                {
                    Id = ownedId_3
                }
            ]
        };

        var existed = await _repository
            .Set<BigEntity>()
            .AnyAsync(e => e.Id == id);

        var created = await _repository.CreateAsync([testEntity], CancellationToken);

        var found = await _repository
            .Set<BigEntity>()
            .AnyAsync(e => e.Id == id);

        Assert.Equal(created, 4);
        Assert.True(testEntity.OwnedLines.All(l => l.OwnerId == testEntity.Id));
        Assert.False(existed);
        Assert.True(found);

        //-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------

        var linesToDelete = repo.Set<TestTableLine>().Where(l => l.Id == ownedId_3);

        var deleted = await repo
            .DeleteAsync(linesToDelete, CancellationToken);

        Assert.Equal(deleted, 1);

        //-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------

        testEntity.OwnedLines = [.. testEntity.OwnedLines.Take(1)];

        var updated = await _repository
            .UpdateAsync([testEntity], CancellationToken);

        var updatedResult = await _repository
            .Set<BigEntity>()
            .Include(e => e.OwnedLines)
            .FirstAsync(e => e.Id == id, CancellationToken);

        Assert.Equal(updated, 1);
        Assert.Equal(updatedResult.OwnedLines.Count, 1);

        //-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------



        //-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------//-----------------------
    }

    [Fact]
    public async Task CrossLoadParallelTesting()
    {
        const int reposCount = 100;
        const int itemsCount = 100;

        int i = 0;

        Task.Run(async () =>
        {
            while (true)
            {
                File.WriteAllText("crossload.log", i.ToString());
                await Task.Delay(1000);
            }
        }).GetType();

        var logLast = () =>
        {
            lock (this) { i++; }
        };

        var dbContext = _dbContext;

        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        var cancellationToken = default(CancellationToken);

        var dbContextSource = TestRepository.CreateDbContextSource(1);

        List<long> createdIds = [];

        var tasks = Enumerable
            .Range(0, reposCount)
            .Select(_ => _repositoryFactory())
            .Select(async repo =>
            {
                var itemsCreateTasks = Enumerable.Range(0, itemsCount).Select(_ =>
                {
                    return Task.Run(async () =>
                    {
                        var newEntity = new BigEntity();
                        await repo.CreateAsync([newEntity], cancellationToken);
                        await repo.CommitChangesAsync(cancellationToken);
                        createdIds.Add(newEntity.Id);
                        logLast();
                    });
                });

                var itemsGetTasks = Enumerable.Range(0, itemsCount)
                    .Select(async _ => await Task.Run(async () => await repo.Set<BigEntity>().Take(50).ToArrayAsync().Then(logLast)));

                await Task.WhenAll(itemsGetTasks);
                await Task.WhenAll(itemsCreateTasks);
            });

        try 
        {
            await Task.WhenAll(tasks);
        }
        finally
        {
            var cleaningRepo = _repositoryFactory();
            var set = cleaningRepo.Set<BigEntity>().Where(e => createdIds.Contains(e.Id));
            await cleaningRepo.DeleteAsync(set, CancellationToken);
            await cleaningRepo.CommitChangesAsync(CancellationToken);
        }
    }

    [Fact]
    public async Task CommitChanges()
    {
        var dbContextSource = TestRepository.CreateDbContextSource(2);

        var repo_1 = new TestRepository(dbContextSource);
        var repo_2 = new TestRepository(dbContextSource);

        var newEntity = new BigEntity();

        var itemsCount = await repo_1.CreateAsync([newEntity], CancellationToken);

        Assert.Equal(itemsCount, 1);

        var entityId = newEntity.Id;

        var fromDbDirectly = await repo_2.Set<BigEntity>().FirstOrDefaultAsync(e => e.Id == entityId);

        Assert.Null(fromDbDirectly);

        await repo_1.CommitChangesAsync(CancellationToken);

        fromDbDirectly = await repo_2.Set<BigEntity>().FirstOrDefaultAsync(e => e.Id == entityId);

        Assert.NotNull(fromDbDirectly);
    }
}

