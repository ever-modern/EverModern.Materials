namespace EverModern.DataProvision.Abstractions;

/// <summary>
/// Represents a persisted entity with an identifier.
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
public interface IEntity<TId>
{
    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    TId Id { get; }

    /// <summary>
    /// Invoked before saving the entity.
    /// </summary>
    void BeforeSave() { }
}
