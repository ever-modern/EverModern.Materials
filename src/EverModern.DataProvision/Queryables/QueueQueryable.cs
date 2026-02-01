using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Collections;
using Microsoft.EntityFrameworkCore.Query;

namespace EverModern.DataProvision.Queryables;


public class QueueQueryable<T> : IQueryable<T>, IAsyncEnumerable<T>, IOrderedQueryable<T>
{
    readonly IQueryable<T> _source;
    readonly Func<CancellationToken, ValueTask<IDisposable>> _lock;

    public QueueQueryable(IQueryable<T> source, Func<CancellationToken, ValueTask<IDisposable>> @lock)
    {
        _source = source;
        _lock = @lock;

        Provider = new StrictlyAsyncQueryProvider((IAsyncQueryProvider)_source.Provider, _lock);
    }

    public Type ElementType => _source.ElementType;

    public Expression Expression => _source.Expression;

    public IQueryProvider Provider { get; }

    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        using var _ = await _lock(cancellationToken);
        await foreach (var item in _source.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public IEnumerator<T> GetEnumerator()
        => throw new NotSupportedException("Only async calls are supported on this IQueryable");

    IEnumerator IEnumerable.GetEnumerator()
        => throw new NotSupportedException("Only async calls are supported on this IQueryable");
}