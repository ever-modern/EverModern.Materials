using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DestallMaterials.WheelProtection.Extensions.SpecialDataTypes
{
    public class MergedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        readonly List<IDictionary<TKey, TValue>> _dictionaries;

        public MergedDictionary(IEnumerable<IDictionary<TKey, TValue>> dictionaries)
        {
            _dictionaries = [.. dictionaries];
        }

        public MergedDictionary(params IDictionary<TKey, TValue>[] dictionaries)
        {
            _dictionaries = [.. dictionaries];
            if (!dictionaries.Any())
            {
                _dictionaries.Add(new Dictionary<TKey, TValue>());
            }
        }

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

        public ICollection<TKey> Keys => [.. _dictionaries.SelectMany(d => d.Keys)];

        public ICollection<TValue> Values => [.. _dictionaries.SelectMany(d => d.Values)];

        public int Count => _dictionaries.Sum(d => d.Count);

        public bool IsReadOnly => _dictionaries.All(d => d.IsReadOnly);

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        public void Add(TKey key, TValue value)
        {
            _dictionaries.First().Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _dictionaries.First().Add(item);
        }

        public void Clear()
        {
            _dictionaries.Clear();
            _dictionaries.Add(new Dictionary<TKey, TValue>());
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionaries.Any(d => d.Contains(item));
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionaries.Any(d => d.ContainsKey(key));
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var dictionary in _dictionaries)
            {
                dictionary.CopyTo(array, arrayIndex);
                arrayIndex += dictionary.Count;
            }
        }

        public bool Remove(TKey key)
        {
            return _dictionaries.Any(_dictionary => _dictionary.Remove(key));
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return _dictionaries.Any(d => d.Remove(item));
        }

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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

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
