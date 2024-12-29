using System;
using System.Collections.Generic;

namespace TrickCore
{
    public class MemoryCacheProvider : ICacheProvider
    {
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();

        public bool Has(string key)
        {
            return _cache.ContainsKey(key);
        }

        public CacheData<T> Get<T>(string key)
        {
            return _cache.TryGetValue(key, out var value) ? (CacheData<T>)value : default;
        }

        public void Set<T>(string key, T value, TimeSpan span)
        {
            _cache[key] = new CacheData<T>
            {
                CacheTime = TrickTime.CurrentServerTime.Add(span),
                MemoryData = value
            };
        }

        public void Remove(string cacheKey)
        {
            _cache.Remove(cacheKey);
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}