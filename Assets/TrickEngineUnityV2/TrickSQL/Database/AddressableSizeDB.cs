using System;
using AnotherTrickWebAPIShared.Dto;
using Dapper.Contrib.Extensions;
using Newtonsoft.Json;

namespace TrickCore
{
    [Preserve, JsonObject, Serializable, Table("addressable_size")]
    public class AddressableSizeDB : IDatabaseObject
    {
        [JsonProperty(PropertyName = "id"), Key] public int Id { get; set; }
        [JsonProperty(PropertyName = "path")] public string Path { get; set; }
        [JsonProperty(PropertyName = "size")] public long Size { get; set; }
        [JsonProperty(PropertyName = "hash")] public string Hash { get; set; }
        [JsonProperty(PropertyName = "platform")] public string Platform { get; set; }

        [JsonProperty(PropertyName = "updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}