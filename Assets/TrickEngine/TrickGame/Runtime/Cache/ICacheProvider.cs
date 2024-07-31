using System;

namespace TrickCore
{
    /// <summary>
    /// The cache provider interface.
    /// </summary>
    public interface ICacheProvider
    {
        bool Has(string key);
        CacheData<T> Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan span);
    }
}