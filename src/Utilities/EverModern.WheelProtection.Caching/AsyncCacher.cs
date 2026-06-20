using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace EverModern.WheelProtection.Caching;

/// <summary>
/// Caches parameterized asynchronous computations.
/// </summary>
/// <typeparam name="TIn">The input type.</typeparam>
/// <typeparam name="TOut">The output type.</typeparam>
public class AsyncCacher<TIn, TOut>
{
    readonly Func<TIn, CancellationToken, Task<TOut>> _source;
    readonly Func<TIn, CachingSettings> _getCachingSettings;

    IDictionary<TIn, CachedValue<Task<TOut>>> _caches;

    readonly Func<TIn, int> _ComputeChecksum;

    /// <summary>
    /// Initializes a new async cache instance.
    /// </summary>
    public AsyncCacher(
        Func<TIn, CancellationToken, Task<TOut>> source,
        Func<TIn, int> computeChecksum,
        Func<TIn, CachingSettings> getCachingSettings
    )
    {
        _getCachingSettings = getCachingSettings;
        _source = source;

        _ComputeChecksum = computeChecksum;

        _caches = new ConcurrentDictionary<TIn, CachedValue<Task<TOut>>>(
            new ByChecksumEqualityComparer(computeChecksum)
        );
    }

    /// <summary>
    /// Invalidates the cache entry for a parameter.
    /// </summary>
    /// <param name="param">The parameter key.</param>
    public void InvalidateCache(TIn param)
    {
        _caches.Remove(param);
    }

    /// <summary>
    /// Invalidates all cache entries.
    /// </summary>
    public void InvalidateCache()
    {
        _caches.Clear();
    }

    /// <summary>
    /// Gets a cached value or computes it if missing.
    /// </summary>
    /// <param name="parameter">The parameter key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<TOut> RunAsync(TIn parameter, CancellationToken cancellationToken)
    {
        if (_caches.TryGetValue(parameter, out var resultTaskCached))
        {
            var now = DateTime.UtcNow;
            if (resultTaskCached.ValidUntil > now)
            {
                var resultTask = resultTaskCached.Value;
                await resultTask.ContinueWith(t => { });

                if (resultTask.IsCompletedSuccessfully)
                {
                    return resultTask.Result;
                }

                AggregateException ex = resultTask.Exception!;
                if (ex.InnerException is TaskCanceledException)
                {
                    var repeatedResult = await RunDirectlyAsync(parameter, cancellationToken);
                    return repeatedResult;
                }

                throw ex;
            }
        }

        var result = await RunDirectlyAsync(parameter, cancellationToken);

        return result;
    }

    /// <summary>
    /// Computes the value and updates the cache.
    /// </summary>
    /// <param name="parameter">The parameter key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public Task<TOut> RunDirectlyAsync(TIn parameter, CancellationToken cancellationToken)
    {
        var result = _source.Invoke(parameter, cancellationToken);
        _caches[parameter] = new(result, DateTime.UtcNow + _getCachingSettings(parameter).Validity);
        EnsureRightCapacity(parameter);
        return result;
    }

    class ByChecksumEqualityComparer : IEqualityComparer<TIn>
    {
        readonly Func<TIn, int> _getChecksum;

        public ByChecksumEqualityComparer(Func<TIn, int> getChecksum)
        {
            _getChecksum = getChecksum;
        }

        public bool Equals(TIn? x, TIn? y) => _getChecksum(x) == _getChecksum(y);

        public int GetHashCode([DisallowNull] TIn obj) => _getChecksum(obj);
    }

    void EnsureRightCapacity(TIn parameter)
    {
        var capacity = _getCachingSettings(parameter).MaxSize;
        if (_caches.Count >= capacity * 2)
        {
            _caches = new ConcurrentDictionary<TIn, CachedValue<Task<TOut>>>(
                _caches.Where(c => c.Value.ValidUntil > DateTime.UtcNow).Take(capacity),
                new ByChecksumEqualityComparer(_ComputeChecksum)
            );
        }
    }
}

/// <summary>
/// Caches parameterless asynchronous computations.
/// </summary>
/// <typeparam name="TOut">The output type.</typeparam>
public class AsyncCacher<TOut>
{
    readonly Func<CancellationToken, Task<TOut>> _source;
    readonly CachingSettings _cachingSettings;

    CachedValue<Task<TOut>> _cache;

    /// <summary>
    /// Initializes a new async cache instance.
    /// </summary>
    public AsyncCacher(
        Func<CancellationToken, Task<TOut>> source,
        CachingSettings cachingSettings
    )
    {
        this._cachingSettings = cachingSettings;
        _source = source;
    }

    /// <summary>
    /// Invalidates the cached value.
    /// </summary>
    public void InvalidateCache()
    {
        _cache = default;
    }

    /// <summary>
    /// Gets a cached value or computes it if missing.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<TOut> RunAsync(CancellationToken cancellationToken)
    {
        var resultTaskCached = _cache;
        var now = DateTime.UtcNow;
        if (resultTaskCached.ValidUntil > now)
        {
            var resultTask = resultTaskCached.Value;
            await resultTask.ContinueWith(t => { });

            if (resultTask.IsCompletedSuccessfully)
            {
                return resultTask.Result;
            }

            AggregateException ex = resultTask.Exception!;
            if (ex.InnerException is TaskCanceledException)
            {
                var repeatedResult = await RunDirectlyAsync(cancellationToken);
                return repeatedResult;
            }

            throw ex;
        }


        var result = await RunDirectlyAsync(cancellationToken);

        return result;
    }

    /// <summary>
    /// Computes the value and updates the cache.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public Task<TOut> RunDirectlyAsync(CancellationToken cancellationToken)
    {
        var result = _source.Invoke(cancellationToken);
        _cache = new(result, DateTime.UtcNow + _cachingSettings.Validity);
        return result;
    }

}
