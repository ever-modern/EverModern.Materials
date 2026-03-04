using System.Linq.Expressions;

namespace EverModern.DataProvision.Abstractions;

/// <summary>
/// Represents a queryable that can be materialized asynchronously.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IAsyncMaterializable<T>
{
    /// <summary>
    /// Gets the expression that defines the logic represented by this Queryable.
    /// </summary>
    /// <remarks>
    /// Use this property to access the underlying expression tree for analysis, transformation, or
    /// execution within LINQ providers or custom query frameworks.
    /// </remarks>
    Expression Expression { get; }

    /// <summary>
    /// Materializes the query to a list asynchronously.
    /// </summary>
    Task<List<T>> ToListAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Materializes the query to an array asynchronously.
    /// </summary>
    Task<T[]> ToArrayAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Materializes the query to a dictionary by key asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="keySelector">The key selector.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<Dictionary<TKey, T>> ToDictionaryAsync<TKey>(
        Func<T, TKey> keySelector,
        CancellationToken cancellationToken = default)
        where TKey : notnull;
    /// <summary>
    /// Materializes the query to a dictionary by key and element asynchronously.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="keySelector">The key selector.</param>
    /// <param name="elementSelector">The element selector.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TKey, TElement>(
        Func<T, TKey> keySelector,
        Func<T, TElement> elementSelector,
        CancellationToken cancellationToken = default)
        where TKey : notnull;
    /// <summary>
    /// Returns the first element in the sequence asynchronously.
    /// </summary>
    Task<T> FirstAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the first element matching the predicate asynchronously.
    /// </summary>
    Task<T> FirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the first element or a default value asynchronously.
    /// </summary>
    Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the first element matching the predicate or a default value asynchronously.
    /// </summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the only element in the sequence asynchronously.
    /// </summary>
    Task<T> SingleAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the only element matching the predicate asynchronously.
    /// </summary>
    Task<T> SingleAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the only element or a default value asynchronously.
    /// </summary>
    Task<T?> SingleOrDefaultAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the only element matching the predicate or a default value asynchronously.
    /// </summary>
    Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    /// <summary>
    /// Determines whether any elements exist asynchronously.
    /// </summary>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Determines whether any elements match the predicate asynchronously.
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the count of elements asynchronously.
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the count of elements matching the predicate asynchronously.
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the long count of elements asynchronously.
    /// </summary>
    Task<long> LongCountAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the long count of elements matching the predicate asynchronously.
    /// </summary>
    Task<long> LongCountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the sum of the projected values asynchronously.
    /// </summary>
    /// <param name="selector">The value selector.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the average of the projected values asynchronously.
    /// </summary>
    /// <param name="selector">The value selector.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<decimal> AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the minimum projected value asynchronously.
    /// </summary>
    /// <typeparam name="TResult">The value type.</typeparam>
    /// <param name="selector">The value selector.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<TResult> MinAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the maximum projected value asynchronously.
    /// </summary>
    /// <typeparam name="TResult">The value type.</typeparam>
    /// <param name="selector">The value selector.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<TResult> MaxAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default);

}
