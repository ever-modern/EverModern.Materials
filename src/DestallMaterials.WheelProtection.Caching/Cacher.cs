using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace DestallMaterials.WheelProtection.Caching;

public abstract class Cacher
{
    public abstract void InvalidateCache();

    public static Cacher<TIn, TOut> Create<TIn, TOut>(
        Func<TIn, TOut> source,
        Func<TIn, CachingSettings> getCachingSettings,
        Func<TIn, int> computeChecksum
    )
    {
        var result = new Cacher<TIn, TOut>(source, computeChecksum, getCachingSettings);
        return result;
    }

    public static AsyncCacher<TIn, TOut> Create<TIn, TOut>(
        Func<TIn, CancellationToken, Task<TOut>> source,
        Func<TIn, CachingSettings> getCachingSettings,
        Func<TIn, int> computeChecksum
    )
    {
        var result = new AsyncCacher<TIn, TOut>(source, computeChecksum, getCachingSettings);
        return result;
    }

    public static AsyncCacher<TOut> Create<TIn, TOut>(
        Func<CancellationToken, Task<TOut>> source,
        CachingSettings cachingSettings
    )
    {
        var result = new AsyncCacher<TOut>(source, cachingSettings);
        return result;
    }

    public static Cacher<TResult> Create<TResult>(
        Func<TResult> source,
        Func<TimeSpan> getCacheLifetime
    )
    {
        var result = new Cacher<TResult>(source, getCacheLifetime);
        return result;
    }
}

public class Cacher<TIn, TOut>
{
    readonly Func<TIn, TOut> _source;
    readonly Func<TIn, CachingSettings> _getCachingSettings;

    IDictionary<TIn, CachedValue<TOut>> _caches;

    readonly Func<TIn, int> _ComputeChecksum;

    public Cacher(
        Func<TIn, TOut> source,
        Func<TIn, int> computeChecksum,
        Func<TIn, CachingSettings> getCachingSettings
    )
    {
        _getCachingSettings = getCachingSettings;
        _source = source;

        _ComputeChecksum = computeChecksum;

        _caches = new ConcurrentDictionary<TIn, CachedValue<TOut>>(
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

    public TOut Run(TIn parameter)
    {
        if (_caches.TryGetValue(parameter, out var result))
        {
            var now = DateTime.UtcNow;
            if (result.ValidUntil > now)
            {
                return result.Value;
            }
        }
        return RunDirectly(parameter);
    }

    public TOut RunDirectly(TIn parameter)
    {
        var result = _source.Invoke(parameter);
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
            _caches = new ConcurrentDictionary<TIn, CachedValue<TOut>>(
                _caches.Where(c => c.Value.ValidUntil > DateTime.UtcNow).Take(capacity),
                new ByChecksumEqualityComparer(_ComputeChecksum)
            );
        }
    }
}


public class Cacher<TOut>
{
    readonly Func<TimeSpan> _getCachingSettings;
    readonly Func<TOut> _source;
    CachedValue<TOut>? _lastCallResult;

    public Cacher(Func<TOut> source, Func<TimeSpan> getCachingSettings)
    {
        _getCachingSettings = getCachingSettings;
        _source = source;
    }

    public void InvalidateCache()
    {
        _lastCallResult = null;
    }

    public TOut Run()
    {
        if (_lastCallResult != null && _lastCallResult.Value.ValidUntil > DateTime.UtcNow)
        {
            return _lastCallResult.Value.Value;
        }
        return RunDirectly();
    }

    public TOut RunDirectly()
    {
        var result = _source();
        _lastCallResult = new(result, DateTime.UtcNow + _getCachingSettings());
        return result;
    }
}
