namespace EverModern.DataProvision.Abstractions;

/// <summary>
/// Exposes the underlying <see cref="IQueryable{T}"/> for internal facade composition.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IQueryableSourceAccessor<T>
{
    /// <summary>
    /// Gets the underlying queryable source.
    /// </summary>
    IQueryable<T> Source { get; }
}
