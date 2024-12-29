using System;
using UnityEngine;

namespace TrickCore
{
    public class PlayerPrefsCacheProvider : ICacheProvider
    {
        public bool Has(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public CacheData<T> Get<T>(string key)
        {
            return PlayerPrefs.GetString(key).DeserializeJsonTryCatch<CacheData<T>>();
        }

        public void Set<T>(string key, T value, TimeSpan span)
        {
            PlayerPrefs.SetString(key, new CacheData<T>()
            {
                CacheTime = TrickTime.CurrentServerTime.Add(span),
                SerializedData = value.SerializeToJsonBase64(),
            }.SerializeToJsonTryCatch(false));
            PlayerPrefs.Save();
        }

        public void Remove(string cacheKey)
        {
            PlayerPrefs.DeleteKey(cacheKey);
        }

        public void Clear()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}