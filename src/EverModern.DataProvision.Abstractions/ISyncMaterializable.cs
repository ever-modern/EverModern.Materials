using System.Linq.Expressions;

namespace EverModern.DataProvision.Abstractions;

/// <summary>
/// Represents a queryable that can be materialized synchronously.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface ISyncMaterializable<T>
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
    /// Materializes the query to a list.
    /// </summary>
    List<T> ToList();
    /// <summary>
    /// Materializes the query to an array.
    /// </summary>
    T[] ToArray();
    /// <summary>
    /// Materializes the query to a dictionary by key.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="keySelector">The key selector.</param>
    Dictionary<TKey, T> ToDictionary<TKey>(Func<T, TKey> keySelector)
        where TKey : notnull;
    /// <summary>
    /// Materializes the query to a dictionary by key and element.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="keySelector">The key selector.</param>
    /// <param name="elementSelector">The element selector.</param>
    Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(
        Func<T, TKey> keySelector,
        Func<T, TElement> elementSelector)
        where TKey : notnull;
    /// <summary>
    /// Returns the first element in the sequence.
    /// </summary>
    T First();
    /// <summary>
    /// Returns the first element matching the predicate.
    /// </summary>
    T First(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Returns the first element or a default value.
    /// </summary>
    T? FirstOrDefault();
    /// <summary>
    /// Returns the first element matching the predicate or a default value.
    /// </summary>
    T? FirstOrDefault(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Returns the only element in the sequence.
    /// </summary>
    T Single();
    /// <summary>
    /// Returns the only element matching the predicate.
    /// </summary>
    T Single(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Returns the only element or a default value.
    /// </summary>
    T? SingleOrDefault();
    /// <summary>
    /// Returns the only element matching the predicate or a default value.
    /// </summary>
    T? SingleOrDefault(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Determines whether any elements exist.
    /// </summary>
    bool Any();
    /// <summary>
    /// Determines whether any elements match the predicate.
    /// </summary>
    bool Any(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Returns the count of elements.
    /// </summary>
    int Count();
    /// <summary>
    /// Returns the count of elements matching the predicate.
    /// </summary>
    int Count(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Returns the long count of elements.
    /// </summary>
    long LongCount();
    /// <summary>
    /// Returns the long count of elements matching the predicate.
    /// </summary>
    long LongCount(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Returns the sum of the projected values.
    /// </summary>
    /// <param name="selector">The value selector.</param>
    decimal Sum(Expression<Func<T, decimal>> selector);
    /// <summary>
    /// Returns the average of the projected values.
    /// </summary>
    /// <param name="selector">The value selector.</param>
    decimal Average(Expression<Func<T, decimal>> selector);
    /// <summary>
    /// Returns the minimum projected value.
    /// </summary>
    /// <typeparam name="TResult">The value type.</typeparam>
    /// <param name="selector">The value selector.</param>
    TResult Min<TResult>(Expression<Func<T, TResult>> selector);
    /// <summary>
    /// Returns the maximum projected value.
    /// </summary>
    /// <typeparam name="TResult">The value type.</typeparam>
    /// <param name="selector">The value selector.</param>
    TResult Max<TResult>(Expression<Func<T, TResult>> selector);
}
