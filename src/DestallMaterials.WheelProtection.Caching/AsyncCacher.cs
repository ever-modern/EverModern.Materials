using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace DestallMaterials.WheelProtection.Caching;

public class AsyncCacher<TIn, TOut>
{
    readonly Func<TIn, CancellationToken, Task<TOut>> _source;
    readonly Func<TIn, CachingSettings> _getCachingSettings;

    IDictionary<TIn, CachedValue<Task<TOut>>> _caches;

    readonly Func<TIn, int> _ComputeChecksum;

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

    public void InvalidateCache(TIn param)
    {
        _caches.Remove(param);
    }

    public void InvalidateCache()
    {
        _caches.Clear();
    }

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

    public Task<TOut> RunDirectlyAsync(TIn parameter, CancellationToken cancellationToken)
    {
        var result = _source.Invoke(parameter, cancellationToken);
        _caches[parameter] = new(result, DateTime.UtcNow + _getCachingSettings(parameter).Validity);
        EnsureRightCapacity(parameter);
        return result;
    }

    private class ByChecksumEqualityComparer : IEqualityComparer<TIn>
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

public class AsyncCacher<TOut>
{
    readonly Func<CancellationToken, Task<TOut>> _source;
    readonly CachingSettings _cachingSettings;

    CachedValue<Task<TOut>> _cache;

    public AsyncCacher(
        Func<CancellationToken, Task<TOut>> source,
        CachingSettings cachingSettings
    )
    {
        this._cachingSettings = cachingSettings;
        _source = source;
    }

    public void InvalidateCache()
    {
        _cache = default;
    }

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

    public Task<TOut> RunDirectlyAsync(CancellationToken cancellationToken)
    {
        var result = _source.Invoke(cancellationToken);
        _cache = new(result, DateTime.UtcNow + _cachingSettings.Validity);
        return result;
    }

}
