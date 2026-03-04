using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace EverModern.WheelProtection.Caching;

/// <summary>
/// Factory for creating cached value helpers.
/// </summary>
public abstract class Cacher
{
    /// <summary>
    /// Invalidates all cached entries.
    /// </summary>
    public abstract void InvalidateCache();

    /// <summary>
    /// Creates a synchronous cache for parameterized values.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="source">The value factory.</param>
    /// <param name="getCachingSettings">The cache settings provider.</param>
    /// <param name="computeChecksum">The checksum function for keys.</param>
    public static Cacher<TIn, TOut> Create<TIn, TOut>(
        Func<TIn, TOut> source,
        Func<TIn, CachingSettings> getCachingSettings,
        Func<TIn, int> computeChecksum
    )
    {
        var result = new Cacher<TIn, TOut>(source, computeChecksum, getCachingSettings);
        return result;
    }

    /// <summary>
    /// Creates an asynchronous cache for parameterized values.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="source">The async value factory.</param>
    /// <param name="getCachingSettings">The cache settings provider.</param>
    /// <param name="computeChecksum">The checksum function for keys.</param>
    public static AsyncCacher<TIn, TOut> Create<TIn, TOut>(
        Func<TIn, CancellationToken, Task<TOut>> source,
        Func<TIn, CachingSettings> getCachingSettings,
        Func<TIn, int> computeChecksum
    )
    {
        var result = new AsyncCacher<TIn, TOut>(source, computeChecksum, getCachingSettings);
        return result;
    }

    /// <summary>
    /// Creates an asynchronous cache for parameterless values.
    /// </summary>
    /// <typeparam name="TIn">Unused type parameter for overload disambiguation.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="source">The async value factory.</param>
    /// <param name="cachingSettings">The cache settings.</param>
    public static AsyncCacher<TOut> Create<TIn, TOut>(
        Func<CancellationToken, Task<TOut>> source,
        CachingSettings cachingSettings
    )
    {
        var result = new AsyncCacher<TOut>(source, cachingSettings);
        return result;
    }

    /// <summary>
    /// Creates a synchronous cache for parameterless values.
    /// </summary>
    /// <typeparam name="TResult">The output type.</typeparam>
    /// <param name="source">The value factory.</param>
    /// <param name="getCacheLifetime">The cache lifetime provider.</param>
    public static Cacher<TResult> Create<TResult>(
        Func<TResult> source,
        Func<TimeSpan> getCacheLifetime
    )
    {
        var result = new Cacher<TResult>(source, getCacheLifetime);
        return result;
    }
}

/// <summary>
/// Caches parameterized synchronous computations.
/// </summary>
/// <typeparam name="TIn">The input type.</typeparam>
/// <typeparam name="TOut">The output type.</typeparam>
public class Cacher<TIn, TOut>
{
    readonly Func<TIn, TOut> _source;
    readonly Func<TIn, CachingSettings> _getCachingSettings;

    IDictionary<TIn, CachedValue<TOut>> _caches;

    readonly Func<TIn, int> _ComputeChecksum;

    /// <summary>
    /// Initializes a new cache instance.
    /// </summary>
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

    /// <summary>
    /// Computes the value and updates the cache.
    /// </summary>
    /// <param name="parameter">The parameter key.</param>
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


/// <summary>
/// Caches parameterless synchronous computations.
/// </summary>
/// <typeparam name="TOut">The output type.</typeparam>
public class Cacher<TOut>
{
    readonly Func<TimeSpan> _getCachingSettings;
    readonly Func<TOut> _source;
    CachedValue<TOut>? _lastCallResult;

    /// <summary>
    /// Initializes a new cache instance.
    /// </summary>
    public Cacher(Func<TOut> source, Func<TimeSpan> getCachingSettings)
    {
        _getCachingSettings = getCachingSettings;
        _source = source;
    }

    /// <summary>
    /// Invalidates the cached value.
    /// </summary>
    public void InvalidateCache()
    {
        _lastCallResult = null;
    }

    /// <summary>
    /// Gets a cached value or computes it if missing.
    /// </summary>
    public TOut Run()
    {
        if (_lastCallResult != null && _lastCallResult.Value.ValidUntil > DateTime.UtcNow)
        {
            return _lastCallResult.Value.Value;
        }
        return RunDirectly();
    }

    /// <summary>
    /// Computes the value and updates the cache.
    /// </summary>
    public TOut RunDirectly()
    {
        var result = _source();
        _lastCallResult = new(result, DateTime.UtcNow + _getCachingSettings());
        return result;
    }
}
