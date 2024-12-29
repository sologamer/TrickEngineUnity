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
        /// <summary>
        /// The cache time.
        /// </summary>
        [JsonProperty("ct")]
        public DateTime CacheTime;

        /// <summary>
        /// The data in base64 format.
        /// </summary>
        [JsonProperty("sd")]
        public string SerializedData;

        /// <summary>
        /// The memory data.
        /// </summary>
        [JsonIgnore]
        public T MemoryData;

        public T GetData() => MemoryData ?? SerializedData.DeserializeJsonBase64TryCatch<T>();

        public bool IsValid()
        {
            return TrickTime.CurrentServerTime < CacheTime;
        }
    }
}