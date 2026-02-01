using EverModern.DataProvision.Queryables;
using EverModern.WheelProtection.Extensions.Tasks;
using EverModern.WheelProtection.Queues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;

namespace EverModern.DataProvision;

public delegate Task<ItemLocker<T>> PooledDbContextSource<T>(CancellationToken cancellationToken);
public delegate Task<ItemLocker<T>> ExpressionExecutingDbContextFactory<T>(
    bool willMakeChanges,
    CancellationToken cancellationToken);

public abstract class EnlightenedRepository<TId, TBaseEntity, TDbContext> : IRepository<TId, TBaseEntity>, IDisposable
    where TDbContext : EnlightenedDbContext<TId, TBaseEntity>
    where TBaseEntity : class, IEntity<TId>
{
    protected readonly PooledDbContextSource<TDbContext> _contextFactory;
    protected readonly TDbContext _sampleContext;

    protected volatile ItemLocker<TDbContext>? _reservedDbContext;
    protected readonly UsageQueue<TDbContext> _contextInnerReservations = new();

    static readonly EntityRelationsSeeker<TId, TBaseEntity> _relationsSeeker = new();

    bool _disposed;
    void CheckDisposed()
    {
        lock (this)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }

    public bool HasChanges { get; protected set; }

    public EnlightenedRepository(
        PooledDbContextSource<TDbContext> contextFactory,
        TDbContext sampleContext)
    {
        _contextFactory = contextFactory;
        _sampleContext = sampleContext;
    }

    public virtual async Task<int> CreateAsync<TCreatedEntity>(IEnumerable<TCreatedEntity> entities, CancellationToken cancellationToken)
        where TCreatedEntity : class, TBaseEntity
    {
        CheckDisposed();
        var (dbContext, alreadyReserved) = await ReserveDbContextInternallyAsync(cancellationToken);
        using var _ = await _contextInnerReservations.OccupyAsync(dbContext, cancellationToken);
        try
        {
            var ids = entities.Select(e =>
            {
                dbContext.AssignValidId(e);
                return e;
            }).ToArray();

            var entityType = typeof(TCreatedEntity);

            var relations = _relationsSeeker.GatherOwningRelations(entityType);

            if (relations.Length > 0)
            {
                var sets = entities
                    .SelectMany(entity => GatherItemDependencies(entity, relations))
                    .GroupBy(dependencies => dependencies.OwnedType)
                    .Select(gr =>
                    {
                        var set = default(IQueryable<IOwnedEntity<TId>>);
                        foreach (var (ownerId, ownedEntityIds, ownedEntityType) in gr)
                        {
                            var ownedIds = ownedEntityIds.Select(e =>
                            {
                                e.OwnerId = ownerId;
                                return e.Id;
                            }).ToArray();

                            var thisSet = ((IQueryable<IOwnedEntity<TId>>)Set(gr.Key))
                                .Where(i => ownedIds.Contains(i.Id) == false && i.OwnerId.Equals(ownerId));

                            set = set is null ? thisSet : set.Concat(thisSet);
                        }

                        return set;
                    }).ToArray();
            }

            dbContext.AddRange(entities);

            int result = -1;
            try
            {
                await EnsureTransactionAsync(dbContext, cancellationToken);
                result = await dbContext.SaveChangesAsync(cancellationToken);
                HasChanges = true;
            }
            catch
            {
                if (alreadyReserved is false)
                {
                    FreeReservedDbContext();
                }
            }
            finally
            {
                dbContext.ChangeTracker.Clear();
            }

            return result;
        }
        catch
        {
            if (alreadyReserved is false)
            {
                FreeReservedDbContext();
            }

            throw;
        }
    }


    public virtual async Task<int> UpdateAsync<TUpdatedEntity>(IEnumerable<TUpdatedEntity> entities, CancellationToken cancellationToken)
        where TUpdatedEntity : class, TBaseEntity
    {
        CheckDisposed();
        var (dbContext, alreadyReserved) = await ReserveDbContextInternallyAsync(cancellationToken);

        try
        {
            await dbContext.AddRangeAsync(entities, cancellationToken);
            var entriesToUpdate = dbContext.ChangeTracker.Entries<TBaseEntity>().ToArray();

            int result = await DeleteUnboundOwnedEntitiesAsync(
                dbContext,
                entities,
                cancellationToken);
            try
            {
                foreach (var entry in entriesToUpdate)
                {
                    var entity = entry.Entity;
                    if (dbContext.IdIsTemporary(entity.Id))
                    {
                        entry.State = EntityState.Added;
                    }
                    else
                    {
                        entry.State = EntityState.Modified;
                    }

                    entity.BeforeSave();
                }

                result += await dbContext.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                dbContext.ChangeTracker.Clear();
            }

            return result;
        }
        catch
        {
            if (alreadyReserved is false)
            {
                FreeReservedDbContext();
            }

            throw;
        }
    }

    public async Task<int> DeleteAsync<TDeletedEntity>(IQueryable<TDeletedEntity> selectQuery, CancellationToken cancellationToken)
        where TDeletedEntity : class, TBaseEntity
    {
        CheckDisposed();
        var (dbContext, alreadyReserved) = await ReserveDbContextInternallyAsync(cancellationToken);

        try
        {
            var deletedCount = await dbContext.ExecuteDeleteAsync(selectQuery, cancellationToken);
            return deletedCount;
        }
        catch
        {
            if (alreadyReserved is false)
            {
                FreeReservedDbContext();
            }

            throw;
        }
    }

    public async ValueTask<bool> CommitChangesAsync(CancellationToken cancellationToken)
    {
        CheckDisposed();
        if (_reservedDbContext is null || !HasChanges)
        {
            return false;
        }

        using var _ = await _contextInnerReservations.OccupyAsync(_reservedDbContext.Item, cancellationToken);
        using var __ = _reservedDbContext;

        var tran = _reservedDbContext.Item.Database.CurrentTransaction;
        if (tran is not null)
        {
            await tran.CommitAsync(cancellationToken);
            tran.Dispose();
        }

        HasChanges = false;

        _reservedDbContext = null;

        return true;
    }

    public async ValueTask<bool> CancelChangesAsync(CancellationToken cancellationToken)
    {
        CheckDisposed();
        if (_reservedDbContext is null || !HasChanges)
        {
            return false;
        }

        using var _ = await _contextInnerReservations.OccupyAsync(_reservedDbContext.Item, cancellationToken);
        using var __ = _reservedDbContext;

        var tran = _reservedDbContext.Item.Database.CurrentTransaction;
        if (tran is not null)
        {
            await tran.RollbackAsync(cancellationToken);
            tran?.Dispose();
        }

        HasChanges = false;

        _reservedDbContext = null;

        return true;
    }

    public IQueryable<TBaseEntity> Set(Type type)
    {
        CheckDisposed();
        return CreatePoolReliantQueryable(type);
    }


    public IQueryable<T> Set<T>()
    {
        CheckDisposed();
        return (IQueryable<T>)Set(typeof(T));
    }


    public void Dispose()
    {
        lock (this)
        {
            if (_disposed)
            {
                return;
            }
            else
            {
                _contextInnerReservations.Dispose();
                if (_reservedDbContext is not null)
                {
                    _reservedDbContext.Dispose();
                    var dbContext = _reservedDbContext.Item;
                    dbContext.Database.CurrentTransaction?.Dispose();
                }

                _disposed = true;
            }
        }
    }


    protected async Task<(TDbContext DbContext, bool AlreadyReserved)> ReserveDbContextInternallyAsync(CancellationToken cancellationToken)
    {
        CheckDisposed();
        bool alreadyReserved = false;
        lock (this)
        {
            alreadyReserved = _reservedDbContext is not null;
            if (alreadyReserved)
            {
                return (_reservedDbContext, true);
            }
        }

        _reservedDbContext = await _contextFactory(cancellationToken);

        var dbContext = _reservedDbContext.Item;
        dbContext.ChangeTracker.Clear();

        return (_reservedDbContext.Item, alreadyReserved);
    }

    protected void FreeReservedDbContext()
    {
        lock (this)
        {
            var dbContextLocker = _reservedDbContext;

            if (dbContextLocker is null)
            {
                return;
            }

            var dbContext = dbContextLocker.Item;

            dbContext.ChangeTracker.Clear();

            dbContext.Database.CurrentTransaction?.Dispose();

            var reservedDbContext = _reservedDbContext;

            _reservedDbContext = null;

            dbContextLocker.Dispose();
        }
    }

    protected async Task<ItemLocker<TDbContext>> GetLockedDbContextAsync(bool willMakeChanges, CancellationToken ct)
    {
        if (_reservedDbContext is not null)
        {
            var innerLocker = await _contextInnerReservations.OccupyAsync(_reservedDbContext.Item, ct);
            return new CallbackItemLocker<TDbContext>(
                _reservedDbContext.Item,
                (item) => innerLocker.Dispose());
        }

        var result = await _contextFactory(ct);

        if (willMakeChanges is true)
        {
            _reservedDbContext = result;
        }

        return result;
    }

    protected async Task ExecuteChangesAsync(Func<CancellationToken, Task> executeExpression, ItemLocker<TDbContext> dbContext, CancellationToken ct)
    {
        await EnsureTransactionAsync(dbContext, ct);
        await executeExpression(ct);
        HasChanges = true;
    }

    async Task<int> DeleteUnboundOwnedEntitiesAsync<TUpdatedEntity>(
        TDbContext dbContext,
       IEnumerable<TUpdatedEntity> entities,
       CancellationToken cancellationToken)
       where TUpdatedEntity : class, TBaseEntity
    {
        var entityType = typeof(TUpdatedEntity);

        var relations = _relationsSeeker.GatherOwningRelations(entityType);

        if (relations.Length < 1)
        {
            return 0;
        }

        var sets = entities
            .SelectMany(entity => GatherItemDependencies(entity, relations))
            .GroupBy(dependencies => dependencies.OwnedType)
            .Select(gr =>
            {
                var exactSet = Set(gr.Key);
                var aggregatedSet = default(IQueryable<IOwnedEntity<TId>>);
                foreach (var (ownerId, ownedEntityIds, ownedEntityType) in gr)
                {
                    var ownedIds = ownedEntityIds.Select(e =>
                    {
                        e.OwnerId = ownerId;
                        return e.Id;
                    });

                    var thisSet = (exactSet as IQueryable<IOwnedEntity<TId>>)
                        .Where(i => ownedIds.Contains(i.Id) == false && i.OwnerId.Equals(ownerId));
                    aggregatedSet = aggregatedSet is null ? thisSet : aggregatedSet.Concat(thisSet);
                }

                var ids = aggregatedSet.Select(e => e.Id);

                var entityTypeResult = exactSet.Where(e => ids.Contains(e.Id));

                return entityTypeResult;
            });

        int result = 0;

        foreach (var set in sets)
        {
            result += await dbContext.ExecuteDeleteAsync(set, cancellationToken);
        }

        return result;
    }

    IQueryable<TBaseEntity> CreatePoolReliantQueryable(Type entityType)
    {
        var creatorMethod = GetQueryableCreatorMethod(entityType);
        var result = creatorMethod(this);
        return result;
    }

    protected static async ValueTask<IDbContextTransaction> EnsureTransactionAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        var transaction = dbContext.Database.CurrentTransaction;
        if (transaction is null || !IsHealthy(transaction))
        {
            transaction?.Dispose();
            transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        return transaction;
    }

    static bool IsHealthy(IDbContextTransaction dbContextTransaction)
    {
        var state = (dbContextTransaction as IInfrastructure<DbTransaction>)?.GetInfrastructure()?.Connection?.State;
        bool result = state is null || state == ConnectionState.Fetching || state == ConnectionState.Open;
        return result;
    }

    static readonly Dictionary<Type, Func<EnlightenedRepository<TId, TBaseEntity, TDbContext>, IQueryable<TBaseEntity>>> _getDbSetShortcuts = [];
    static Func<EnlightenedRepository<TId, TBaseEntity, TDbContext>, IQueryable<TBaseEntity>> GetQueryableCreatorMethod(
        Type entityType)
    {
        if (_getDbSetShortcuts.TryGetValue(entityType, out var queryableCreation) is false)
        {
            lock (_getDbSetShortcuts)
            {
                var dbSetType = typeof(DbSet<>).MakeGenericType(entityType);

                var dbSetProperty = typeof(TDbContext).GetProperties().FirstOrDefault(p => p.PropertyType == dbSetType)
                    ?? throw new InvalidOperationException($"{dbSetType} for entity {entityType} isn't found on {typeof(TDbContext)}.");

                var getMethod = dbSetProperty.GetGetMethod()
                    ?? throw new InvalidOperationException();

                var getDbSetShortcut = (TDbContext dbContext) => getMethod.Invoke(dbContext, []) as IQueryable<TBaseEntity>
                    ?? throw new InvalidOperationException($"{dbSetProperty} did not return IQueryable<{entityType}>.");

                var resultTypeConstructor = typeof(PoolReliantQueryable<,>)
                    .MakeGenericType(entityType, typeof(TDbContext))
                    .GetConstructors()
                    .First(c => c.GetParameters().Length == 4);

                var getCorrectQueryableFunc = getMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(TDbContext), dbSetType));

                queryableCreation = (EnlightenedRepository<TId, TBaseEntity, TDbContext> repo) => resultTypeConstructor.Invoke([
                        getDbSetShortcut(repo._sampleContext),
                        getCorrectQueryableFunc,
                        (ExpressionExecutingDbContextFactory<TDbContext>)((bool willMakeChanges, CancellationToken ct) =>
                        {
                            lock (repo)
                            {
                                if (repo._reservedDbContext is null && (willMakeChanges is false || repo._reservedDbContext is null))
                                {
                                    return repo._contextFactory(ct);
                                }

                                return repo
                                    .ReserveDbContextInternallyAsync(ct)
                                    .ThenAsync(async t =>
                                    {
                                        var (dbContext, alreadyReserved) = t;
                                        dbContext.ChangeTracker.Clear();

                                        await EnsureTransactionAsync(dbContext, ct);

                                        var lockerTask = await repo._contextInnerReservations.OccupyAsync(dbContext, ct)
                                            .Then(locker => new CallbackItemLocker<TDbContext>(locker.Item, (l) =>
                                            {
                                                lock (repo)
                                                {
                                                    if (willMakeChanges && alreadyReserved is false && repo.HasChanges is false)
                                                    {
                                                        repo.FreeReservedDbContext();
                                                    }

                                                    locker.Dispose();
                                                }
                                            }));

                                        return (ItemLocker<TDbContext>)lockerTask;
                                    });
                            }
                        }),
                        (ExecuteChanges<TDbContext>)repo.ExecuteChangesAsync
                    ]) as IQueryable<TBaseEntity>
                    ?? throw new InvalidOperationException("Invalid type returned.");

                _getDbSetShortcuts[entityType] = queryableCreation;
            }
        }

        return queryableCreation;
    }
    static IEnumerable<EntityRelationsSeeker<TId, TBaseEntity>.EntityRelation> Unwrap(IEnumerable<EntityRelationsSeeker<TId, TBaseEntity>.EntityRelation> relations)
       => relations.Concat(relations.SelectMany(rel => Unwrap(rel.InnerRelations))).Select(rel => rel with { InnerRelations = [] });

    static IEnumerable<(TId Owner, IEnumerable<IOwnedEntity<TId>> OwnedIds, Type OwnedType)> GatherItemDependencies(
        TBaseEntity entity, IEnumerable<EntityRelationsSeeker<TId, TBaseEntity>.EntityRelation> relations)
    {
        foreach (var relation in relations)
        {
            var owner = entity;
            var ownedEntities = relation.GetMany?.Invoke(owner) ?? [relation.GetOne!(owner)];
            var ownedType = relation.OwnedEntity;

            yield return (owner.Id, [.. ownedEntities], ownedType);

            foreach (var innerRelation in ownedEntities)
            {
                foreach (var innerResult in ownedEntities.SelectMany(o => GatherItemDependencies((TBaseEntity)o, relation.InnerRelations)))
                {
                    yield return innerResult;
                }
            }
        }
    }
}