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
        private T? _memoryData;

        [JsonIgnore]
        public T MemoryData
        {
            get => _memoryData.HasValue ? _memoryData.Value : default;
            set => _memoryData = value;
        }

        public T GetData()
        {
            return _memoryData.HasValue
                ? _memoryData.Value
                : SerializedData.DeserializeJsonBase64TryCatch<T>();
        }

        public bool IsValid()
        {
            return TrickTime.CurrentServerTime < CacheTime;
        }
    }

}