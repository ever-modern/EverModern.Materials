using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace EverModern.DataProvision;

public interface IEntity<TId>
{
    TId Id { get; }

    void BeforeSave() { }
}

public interface IOwnedEntity<TId> : IEntity<TId>
{
    TId OwnerId { get; set; }
}

public interface IOwnedEntity<TId, in TOwner> : IOwnedEntity<TId>
    where TOwner : IEntity<TId>
{
}

public abstract class EnlightenedDbContext<TId, TBaseEntity> : DbContext
    where TBaseEntity : class, IEntity<TId>
{
    readonly EventManager _changesHaveBeenMade = new();
    readonly EventManager _makingChanges = new();
    readonly TaskCompletionSource _isConfigured = new();

    public Task WhenConfigured => _isConfigured.Task;

    public IEventSubscriber ChangesHaveBeenMade => _changesHaveBeenMade;
    public IEventSubscriber MakingChanges => _makingChanges;

    static readonly EntityRelationsSeeker<TId, TBaseEntity> _relationsSeeker = new();
    protected EnlightenedDbContext()
    {
    }

    protected EnlightenedDbContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    public async Task<int> ExecuteUpdateAsync<TUpdatedEntity>(
        IQueryable<TUpdatedEntity> itemsToUpdate,
        Expression<Func<SetPropertyCalls<TUpdatedEntity>, SetPropertyCalls<TUpdatedEntity>>> setters,
        CancellationToken cancellationToken)
        where TUpdatedEntity : class, TBaseEntity
    {
        _makingChanges.Fire();
        var result = await ExecuteUpdateInnerAsync(itemsToUpdate, setters, cancellationToken);
        _changesHaveBeenMade.Fire();
        return result;
    }

    public async Task<int> ExecuteDeleteAsync<TDeletedEntity>(IQueryable<TDeletedEntity> entities, CancellationToken cancellationToken)
        where TDeletedEntity : class, TBaseEntity
    {
        _makingChanges.Fire();
        var result = await ExecuteDeleteInnerAsync(entities, cancellationToken);
        _changesHaveBeenMade.Fire();
        return result;
    }

    protected override sealed void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TBaseEntity>().UseTpcMappingStrategy();

        var allEntities = typeof(TBaseEntity).Assembly
            .GetTypes()
            .Where(t => t.IsAbstract == false && t.IsAssignableTo(typeof(TBaseEntity)))
            .ToArray();

        OnModelCreatingFurther(modelBuilder);

        _isConfigured.SetResult();
    }

    protected virtual void OnModelCreatingFurther(ModelBuilder modelBuilder)
    {
    }

    protected abstract Task<int> ExecuteRawSqlAsync(string sql, CancellationToken cancellationToken);

    public abstract bool IdIsTemporary(TId id);

    public abstract void AssignValidId(TBaseEntity entity);

    protected abstract Task<int> ExecuteUpdateInnerAsync<T>(
        IQueryable<T> query,
        Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setters,
        CancellationToken cancellationToken = default)
        where T : TBaseEntity;

    protected abstract Task<int> ExecuteDeleteInnerAsync<T>(
        IQueryable<T> query,
        CancellationToken cancellationToken = default)
        where T : TBaseEntity;
}
