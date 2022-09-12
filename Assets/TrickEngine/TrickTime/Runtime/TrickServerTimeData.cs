using System;
using Newtonsoft.Json;

namespace TrickCore
{
    [Preserve, JsonObject, Serializable]
    public sealed class TrickServerTimeData : ITrickTimeServerTime
    {
        [Preserve, JsonProperty(PropertyName = "server_time")] public DateTime FetchedServerTime { get; set; }
    }
}