namespace DestallMaterials.EnlightenedDataProvision;

public interface IRepository<TId, TEntity>
    where TEntity : class, IEntity<TId>
{
    IQueryable<TEntity> Set(Type type);

    IQueryable<T> Set<T>();

    ValueTask<bool> CommitChangesAsync(CancellationToken cancellationToken);

    ValueTask<bool> CancelChangesAsync(CancellationToken cancellationToken);

    Task<int> CreateAsync<TCreatedEntity>(IEnumerable<TCreatedEntity> newEntities, CancellationToken cancellationToken)
        where TCreatedEntity : class, TEntity;

    Task<int> UpdateAsync<TUpdatedEntity>(IEnumerable<TUpdatedEntity> existingEntities, CancellationToken cancellationToken)
        where TUpdatedEntity : class, TEntity;
    Task<int> DeleteAsync<TDeletedEntity>(IQueryable<TDeletedEntity> selectQuery, CancellationToken cancellationToken)
        where TDeletedEntity : class, TEntity;

    bool HasChanges { get; }
}