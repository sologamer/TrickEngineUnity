using System;

namespace TrickCore
{
    public interface ICacheProvider
    {
        bool Has(string key);
        PlayerPrefsCacheProvider.CacheData<T> Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan span);
    }
}