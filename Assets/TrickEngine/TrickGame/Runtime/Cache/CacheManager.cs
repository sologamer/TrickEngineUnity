using System;

namespace TrickCore
{
    public class CacheManager : MonoSingleton<CacheManager>
    {
        private ICacheProvider _cacheProvider;

        protected override void Initialize()
        {
            base.Initialize();
            _cacheProvider = new MemoryCacheProvider();
        }

        public void SetCacheProvider(ICacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider;
        }

        public T GetOrFetch<T>(string cacheKey, TimeSpan span, Func<T> fetchCallback)
        {
            if (_cacheProvider.Has(cacheKey))
            {
                // get from the cache and check if valid or not
                var cache = _cacheProvider.Get<T>(cacheKey);
                if (cache.IsValid()) return cache.GetData();
            }

            var data = fetchCallback.Invoke();
            _cacheProvider.Set(cacheKey, data, span);
            return data;
        }
    }
}