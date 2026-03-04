using EverModern.DataProvision.Abstractions;

namespace EverModern.DataProvision;

/// <summary>
/// Represents a repository for entities with a common identifier type.
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
/// <typeparam name="TEntity">The base entity type.</typeparam>
public interface IRepository<TId, TEntity>
    where TEntity : class, IEntity<TId>
{
    /// <summary>
    /// Gets a queryable set for the specified entity type.
    /// </summary>
    /// <param name="type">The entity type.</param>
    IQueryable<TEntity> Set(Type type);

    /// <summary>
    /// Gets a queryable set for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    IQueryable<T> Set<T>();

    /// <summary>
    /// Commits any reserved changes.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask<bool> CommitChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Cancels any reserved changes.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask<bool> CancelChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates the specified entities.
    /// </summary>
    /// <typeparam name="TCreatedEntity">The entity type.</typeparam>
    /// <param name="newEntities">The entities to create.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<int> CreateAsync<TCreatedEntity>(IEnumerable<TCreatedEntity> newEntities, CancellationToken cancellationToken)
        where TCreatedEntity : class, TEntity;

    /// <summary>
    /// Updates the specified entities.
    /// </summary>
    /// <typeparam name="TUpdatedEntity">The entity type.</typeparam>
    /// <param name="existingEntities">The entities to update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<int> UpdateAsync<TUpdatedEntity>(IEnumerable<TUpdatedEntity> existingEntities, CancellationToken cancellationToken)
        where TUpdatedEntity : class, TEntity;
    /// <summary>
    /// Deletes entities selected by the query.
    /// </summary>
    /// <typeparam name="TDeletedEntity">The entity type.</typeparam>
    /// <param name="selectQuery">The query selecting entities to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<int> DeleteAsync<TDeletedEntity>(IQueryable<TDeletedEntity> selectQuery, CancellationToken cancellationToken)
        where TDeletedEntity : class, TEntity;

    /// <summary>
    /// Gets whether changes are pending.
    /// </summary>
    bool HasChanges { get; }
}