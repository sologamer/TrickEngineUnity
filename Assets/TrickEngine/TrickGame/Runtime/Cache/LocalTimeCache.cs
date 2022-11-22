using System;

namespace TrickCore
{
    public class LocalTimeCache : PlayerPrefsCacheProvider
    {
        public T GetOrFetch<T>(string cacheKey, TimeSpan span, Func<T> fetchCallback)
        {
            if (Has(cacheKey))
            {
                // get from the cache and check if valid or not
                var cache = Get<T>(cacheKey);
                if (cache.IsValid())
                    return cache.Data.DeserializeJsonBase64TryCatch<T>();
            }

            var data = fetchCallback.Invoke();
            Set(cacheKey, data, span);
            return data;
        }
    }
}