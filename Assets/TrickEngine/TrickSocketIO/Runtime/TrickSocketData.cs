using System;
using Newtonsoft.Json;

namespace TrickCore
{
    [Serializable, JsonObject]
    public class TrickSocketData
    {
        [JsonProperty("eventName")] public string EventName { get; set; }
        [JsonProperty("payload")] public string Payload { get; set; }

        /// <summary>
        /// Gets the payload casted to a type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetPayloadAs<T>() => Payload != null ? Payload.DeserializeJsonTryCatch<T>() : default;
    }
}