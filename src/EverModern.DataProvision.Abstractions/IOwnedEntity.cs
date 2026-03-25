using System.Linq.Expressions;

namespace EverModern.DataProvision.Abstractions;

/// <summary>
/// Represents an owned entity with an identifier.
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
public interface IOwnedEntity<TId> : IEntity<TId>
{
    /// <summary>
    /// Gets or sets the owner identifier.
    /// </summary>
    TId OwnerId { get; set; }
}

/// <summary>
/// Represents an entity owned by a specific owner type.
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
/// <typeparam name="TOwner">The owner type.</typeparam>
public interface IOwnedEntity<TId, in TOwner> : IOwnedEntity<TId>
    where TOwner : IEntity<TId>
{
}
