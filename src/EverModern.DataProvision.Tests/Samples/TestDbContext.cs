using EverModern.WheelProtection.DataWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace EverModern.DataProvision.Tests.Samples;

public class TestDbContext : EnlightenedDbContext<long, TestBaseEntity>
{
    static readonly IdsGenerator _idsGenerator = new();
    readonly string _connectionString;

    public TestDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<BigEntity> Entities { get; set; }

    public DbSet<TestTableLine> OwnedLines { get; set; }

    public static TestDbContext ForFileName(string fileName) => new($"Data source={fileName}");

    protected override void OnConfiguring(DbContextOptionsBuilder dbContextOptionsBuilder)
    {
        dbContextOptionsBuilder.UseSqlite(_connectionString);
    }

    protected override Task<int> ExecuteDeleteInnerAsync<TDeletedEntity>(
        IQueryable<TDeletedEntity> entities,
        CancellationToken cancellationToken)
        => entities.ExecuteDeleteAsync(cancellationToken);

    protected override Task<int> ExecuteRawSqlAsync(string sql, CancellationToken cancellationToken)
        => this.Database.ExecuteSqlRawAsync(sql, cancellationToken);

    protected override Task<int> ExecuteUpdateInnerAsync<TUpdatedEntity>(
        IQueryable<TUpdatedEntity> entitiesToUpdate,
        Expression<Func<SetPropertyCalls<TUpdatedEntity>,
        SetPropertyCalls<TUpdatedEntity>>> setters,
        CancellationToken cancellationToken)
        => entitiesToUpdate.ExecuteUpdateAsync(setters, cancellationToken);

    public override bool IdIsTemporary(long id) => id < 1;

    public override void AssignValidId(TestBaseEntity entity)
    {
        if (entity.Id > 0)
        {
            return;
        }

        entity.Id = _idsGenerator.Generate();
    }
}
