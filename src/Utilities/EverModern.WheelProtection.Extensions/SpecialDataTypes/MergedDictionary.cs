using System.Collections;

namespace EverModern.WheelProtection.Extensions.SpecialDataTypes
{
    /// <summary>
    /// Presents multiple dictionaries as a merged view.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public class MergedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        readonly List<IDictionary<TKey, TValue>> _dictionaries;

        /// <summary>
        /// Initializes a new instance from a sequence of dictionaries.
        /// </summary>
        /// <param name="dictionaries">The dictionaries to merge.</param>
        public MergedDictionary(IEnumerable<IDictionary<TKey, TValue>> dictionaries)
        {
            _dictionaries = [.. dictionaries];
        }

        /// <summary>
        /// Initializes a new instance from an array of dictionaries.
        /// </summary>
        /// <param name="dictionaries">The dictionaries to merge.</param>
        public MergedDictionary(params IDictionary<TKey, TValue>[] dictionaries)
        {
            _dictionaries = [.. dictionaries];
            if (!dictionaries.Any())
            {
                _dictionaries.Add(new Dictionary<TKey, TValue>());
            }
        }

        /// <inheritdoc />
        public TValue this[TKey key]
        {
            get
            {
                var d = _dictionaries.FirstOrDefault(d => d.ContainsKey(key));
                if (d == null)
                {
                    throw new KeyNotFoundException();
                }
                return d[key];
            }
            set
            {
                var d = _dictionaries.FirstOrDefault(d => d.ContainsKey(key));
                if (d == null)
                {
                    throw new KeyNotFoundException();
                }
                d[key] = value;
            }
        }

        /// <inheritdoc />
        public ICollection<TKey> Keys => [.. _dictionaries.SelectMany(d => d.Keys)];

        /// <inheritdoc />
        public ICollection<TValue> Values => [.. _dictionaries.SelectMany(d => d.Values)];

        /// <inheritdoc />
        public int Count => _dictionaries.Sum(d => d.Count);

        /// <inheritdoc />
        public bool IsReadOnly => _dictionaries.All(d => d.IsReadOnly);

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        /// <inheritdoc />
        public void Add(TKey key, TValue value)
        {
            _dictionaries.First().Add(key, value);
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _dictionaries.First().Add(item);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _dictionaries.Clear();
            _dictionaries.Add(new Dictionary<TKey, TValue>());
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionaries.Any(d => d.Contains(item));
        }

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            return _dictionaries.Any(d => d.ContainsKey(key));
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var dictionary in _dictionaries)
            {
                dictionary.CopyTo(array, arrayIndex);
                arrayIndex += dictionary.Count;
            }
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            return _dictionaries.Any(_dictionary => _dictionary.Remove(key));
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return _dictionaries.Any(d => d.Remove(item));
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default;
            foreach (var dictionary in _dictionaries)
            {
                if (dictionary.TryGetValue(key, out var val))
                {
                    value = val;
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            for (int i = 0; i < _dictionaries.Count; i++)
            {
                var dictionary = _dictionaries[i];
                foreach (var kv in dictionary)
                {
                    yield return kv;
                }
            }
        }
    }
}
