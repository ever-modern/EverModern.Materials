using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace EverModern.DataProvision.Abstractions;

/// <summary>
/// Exposes Entity Framework specific query capabilities.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IEntityFrameworkQueryable<T> :
    IReadOnlyQueryable<T>, IReadOnlyOrderedQueryable<T>,
    IReadOnlyAsyncQueryable<T>, IReadOnlyAsyncOrderedQueryable<T>,
    IAsyncOnlyQueryable<T>, IAsyncOnlyOrderedQueryable<T>
{
    /// <summary>
    /// Includes a related navigation property.
    /// </summary>
    /// <typeparam name="TProperty">The navigation property type.</typeparam>
    /// <param name="navigationPropertyPath">The navigation property path.</param>
    IEntityFrameworkQueryable<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath);
    /// <summary>
    /// Disables change tracking for the query.
    /// </summary>
    IEntityFrameworkQueryable<T> AsNoTracking();
    /// <summary>
    /// Enables change tracking for the query.
    /// </summary>
    IEntityFrameworkQueryable<T> AsTracking();
    /// <summary>
    /// Executes the query as split queries.
    /// </summary>
    IEntityFrameworkQueryable<T> AsSplitQuery();
    /// <summary>
    /// Executes the query as a single query.
    /// </summary>
    IEntityFrameworkQueryable<T> AsSingleQuery();
    /// <summary>
    /// Adds a tag to the query.
    /// </summary>
    /// <param name="tag">The tag.</param>
    IEntityFrameworkQueryable<T> TagWith(string tag);
    /// <summary>
    /// Executes a set-based update asynchronously.
    /// </summary>
    /// <param name="setPropertyCalls">The update builder.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<int> ExecuteUpdateAsync(
        Action<UpdateSettersBuilder<T>> setPropertyCalls,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Executes a set-based delete asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<int> ExecuteDeleteAsync(CancellationToken cancellationToken = default);
}
