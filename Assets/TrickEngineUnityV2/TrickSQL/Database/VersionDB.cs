using System;
using AnotherTrickWebAPIShared.Dto;
using Dapper.Contrib.Extensions;
using Newtonsoft.Json;

namespace TrickCore
{
    [Preserve, JsonObject, Serializable, Table("versions")]
    public class VersionDB : IDatabaseObject
    {
        [JsonProperty(PropertyName = "type"), Key] public string Type { get; set; }
        [JsonProperty(PropertyName = "version")] public int Version { get; set; }
        [JsonProperty(PropertyName = "updated_at")] public DateTime UpdatedAt { get; set; }
    }
}