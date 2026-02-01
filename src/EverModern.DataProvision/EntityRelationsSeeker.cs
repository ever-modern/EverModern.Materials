using System.Data;
using System.Reflection;

namespace EverModern.DataProvision;

class EntityRelationsSeeker<TId, TEntity>
{
    static readonly Dictionary<Type, EntityRelation[]> _gatherDependentEntities = [];

    public EntityRelation? GatherOwningRelationInfo(Type ownerEntity, PropertyInfo property)
    {
        var ownedType = property.PropertyType;

        var ownedEntityInterface = typeof(IOwnedEntity<,>).MakeGenericType(typeof(TId), ownerEntity);

        if (ownedType.IsAssignableTo(ownedEntityInterface))
        {
            return new(
                Owner: ownerEntity,
                OwnedEntity: ownedType,
                Property: property,
                GetOne: (owner) => (IOwnedEntity<TId>)property.GetValue(owner),
                GetMany: null,
                InnerRelations: GatherOwningRelations(ownedType));
        }

        if (ownedType.IsAssignableTo(typeof(System.Collections.IEnumerable)) && ownedType.GenericTypeArguments.Length == 1)
        {
            ownedType = ownedType.GenericTypeArguments[0];

            if (ownedType.IsAssignableTo(ownedEntityInterface))
            {
                ownedEntityInterface = ownedType;

                return new(
                    Owner: ownerEntity,
                    OwnedEntity: ownedType,
                    Property: property,
                    GetOne: null,
                    GetMany: (owner) => (IEnumerable<IOwnedEntity<TId>>)property.GetValue(owner),
                    InnerRelations: GatherOwningRelations(ownedType));
            }
        }

        return null;
    }

    public EntityRelation[] GatherOwningRelations(Type entityType)
    {
        if (_gatherDependentEntities.TryGetValue(entityType, out var ownedEntitityProperties) is false)
        {
            lock (_gatherDependentEntities)
            {
                ownedEntitityProperties = [..entityType
                    .GetProperties()
                    .Select(prop => GatherOwningRelationInfo(entityType, prop)!)
                    .Where(p => p is not null)];

                _gatherDependentEntities[entityType] = ownedEntitityProperties;
            }
        }

        return ownedEntitityProperties;
    }

    public record EntityRelation(
        Type Owner,
        Type OwnedEntity,
        PropertyInfo Property,
        Func<TEntity, IOwnedEntity<TId>>? GetOne,
        Func<TEntity, IEnumerable<IOwnedEntity<TId>>>? GetMany,
        IReadOnlyList<EntityRelation> InnerRelations
    );
}
