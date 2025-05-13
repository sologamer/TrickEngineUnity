using System;
using Newtonsoft.Json;

namespace TrickCore
{
    /// <summary>
    /// The cache data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [JsonObject]
    public struct CacheData<T>
    {
        [JsonProperty("ct")]
        public DateTime CacheTime;

        [JsonProperty("sd")]
        public string SerializedData;

        [JsonIgnore]
        private T _memoryData;

        [JsonIgnore]
        private bool _hasMemoryData;

        [JsonIgnore]
        public T MemoryData
        {
            get => _memoryData;
            set
            {
                _memoryData = value;
                _hasMemoryData = true;
            }
        }

        public T GetData()
        {
            return _hasMemoryData
                ? _memoryData
                : SerializedData.DeserializeJsonBase64TryCatch<T>();
        }

        public bool IsValid()
        {
            return TrickTime.CurrentServerTime < CacheTime;
        }
    }


}