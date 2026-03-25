using EverModern.DataProvision.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace EverModern.DataProvision;


/// <summary>
/// Base DbContext with conventions and helpers for the EverModern data provision layer.
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
/// <typeparam name="TBaseEntity">The base entity type.</typeparam>
public abstract class EnlightenedDbContext<TId, TBaseEntity> : DbContext
    where TBaseEntity : class, IEntity<TId>
{
    readonly EventManager _changesHaveBeenMade = new();
    readonly EventManager _makingChanges = new();
    readonly TaskCompletionSource _isConfigured = new();

    /// <summary>
    /// Gets a task that completes when the model is configured.
    /// </summary>
    public Task WhenConfigured => _isConfigured.Task;

    /// <summary>
    /// Gets an event stream fired after changes are made.
    /// </summary>
    public IEventSubscriber ChangesHaveBeenMade => _changesHaveBeenMade;
    /// <summary>
    /// Gets an event stream fired before changes are made.
    /// </summary>
    public IEventSubscriber MakingChanges => _makingChanges;

    static readonly EntityRelationsSeeker<TId, TBaseEntity> _relationsSeeker = new();
    /// <summary>
    /// Initializes a new instance of the context.
    /// </summary>
    protected EnlightenedDbContext()
    {
    }

    /// <summary>
    /// Initializes a new instance of the context with options.
    /// </summary>
    /// <param name="dbContextOptions">The context options.</param>
    protected EnlightenedDbContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    /// <summary>
    /// Executes a set-based update and raises change events.
    /// </summary>
    /// <typeparam name="TUpdatedEntity">The entity type to update.</typeparam>
    /// <param name="itemsToUpdate">The query selecting entities to update.</param>
    /// <param name="setters">The update setters.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task<int> ExecuteUpdateAsync<TUpdatedEntity>(
        IQueryable<TUpdatedEntity> itemsToUpdate,
       Action<UpdateSettersBuilder<TUpdatedEntity>> setters,
        CancellationToken cancellationToken)
        where TUpdatedEntity : class, TBaseEntity
    {
        _makingChanges.Fire();
        var result = await ExecuteUpdateInnerAsync(itemsToUpdate, setters, cancellationToken);
        _changesHaveBeenMade.Fire();
        return result;
    }

    /// <summary>
    /// Executes a set-based delete and raises change events.
    /// </summary>
    /// <typeparam name="TDeletedEntity">The entity type to delete.</typeparam>
    /// <param name="entities">The query selecting entities to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task<int> ExecuteDeleteAsync<TDeletedEntity>(IQueryable<TDeletedEntity> entities, CancellationToken cancellationToken)
        where TDeletedEntity : class, TBaseEntity
    {
        _makingChanges.Fire();
        var result = await ExecuteDeleteInnerAsync(entities, cancellationToken);
        _changesHaveBeenMade.Fire();
        return result;
    }

    /// <inheritdoc />
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

    /// <summary>
    /// Allows further model configuration in derived contexts.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected virtual void OnModelCreatingFurther(ModelBuilder modelBuilder)
    {
    }

    /// <summary>
    /// Executes raw SQL and returns the number of rows affected.
    /// </summary>
    /// <param name="sql">The SQL string.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    protected abstract Task<int> ExecuteRawSqlAsync(string sql, CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether an identifier is temporary.
    /// </summary>
    /// <param name="id">The identifier.</param>
    public abstract bool IdIsTemporary(TId id);

    /// <summary>
    /// Assigns a valid identifier to the entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    public abstract void AssignValidId(TBaseEntity entity);

    /// <summary>
    /// Executes a set-based update without raising change events.
    /// </summary>
    protected abstract Task<int> ExecuteUpdateInnerAsync<T>(
        IQueryable<T> query,
        Action<UpdateSettersBuilder<T>> setters,
        CancellationToken cancellationToken = default)
        where T : TBaseEntity;

    /// <summary>
    /// Executes a set-based delete without raising change events.
    /// </summary>
    protected abstract Task<int> ExecuteDeleteInnerAsync<T>(
        IQueryable<T> query,
        CancellationToken cancellationToken = default)
        where T : TBaseEntity;
}
