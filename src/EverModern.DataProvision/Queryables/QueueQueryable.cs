using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Collections;
using Microsoft.EntityFrameworkCore.Query;

namespace EverModern.DataProvision.Queryables;


/// <summary>
/// IQueryable implementation that enforces async-only execution with a shared lock.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public class QueueQueryable<T> : IQueryable<T>, IAsyncEnumerable<T>, IOrderedQueryable<T>
{
    readonly IQueryable<T> _source;
    readonly Func<CancellationToken, ValueTask<IDisposable>> _lock;

    /// <summary>
    /// Initializes a new instance of the queue-backed queryable.
    /// </summary>
    /// <param name="source">The underlying queryable.</param>
    /// <param name="lock">The async lock factory.</param>
    public QueueQueryable(IQueryable<T> source, Func<CancellationToken, ValueTask<IDisposable>> @lock)
    {
        _source = source;
        _lock = @lock;

        Provider = new StrictlyAsyncQueryProvider((IAsyncQueryProvider)_source.Provider, _lock);
    }

    /// <inheritdoc />
    public Type ElementType => _source.ElementType;

    /// <inheritdoc />
    public Expression Expression => _source.Expression;

    /// <inheritdoc />
    public IQueryProvider Provider { get; }

    /// <inheritdoc />
    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        using var _ = await _lock(cancellationToken);
        await foreach (var item in _source.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
        => throw new NotSupportedException("Only async calls are supported on this IQueryable");

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
        => throw new NotSupportedException("Only async calls are supported on this IQueryable");
}