using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace TrickCore
{
    public static class JsonExtensions
    {
        public static readonly List<JsonConverter> Converters = new List<JsonConverter>()
        {
            new IsoDateTimeConverter(),
            new QuaternionJsonConverter(),
            new Vector2JsonConverter(),
            new Vector2IntJsonConverter(),
            new Vector3JsonConverter(),
            new Vector3IntJsonConverter(),
            new Vector4JsonConverter(),
        };
        
        public static NullValueHandling? OverrideNullValueHandling = null;
        public static DefaultValueHandling? OverrideDefaultValueHandling = null;
        public static ReferenceLoopHandling? OverrideReferenceLoopHandling = null;

        private static DefaultContractResolver TrickContractResolver { get; } = new DefaultContractResolver()
        {
            IgnoreSerializableAttribute = false,
        };
        
        public class TrickIgnorePropertiesResolver : DefaultContractResolver
        {
            
            public TrickIgnorePropertiesResolver()
            {
                
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);
                if (member is PropertyInfo propertyInfo)
                {
                    var attribute = propertyInfo.GetCustomAttribute<JsonPropertyAttribute>();
                    property.ShouldSerialize = _ => attribute != null;
                }
                return property;
            }
        }

        /// <summary>
        /// Deserialize the json string to a json object and catch any exceptions
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json">The json string</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static T DeserializeJsonTryCatch<T>(this string json, IContractResolver contractResolver = null)
        {
            try
            {
                return DeserializeJson<T>(json, true, contractResolver);
            }
            catch (Exception ex)
            {
                Logger.Game.LogException(ex);
                return default;
            }
        }

        /// <summary>
        /// Deserialize the json string to a json object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json">The json string</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static T DeserializeJson<T>(this string json, IContractResolver contractResolver = null)
        {
            return DeserializeJson<T>(json, true, contractResolver);
        }

        /// <summary>
        /// Deserialize the json string to a json object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json">The json string</param>
        /// <param name="includeNull">True if we include null fields</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static T DeserializeJson<T>(this string json, bool includeNull, IContractResolver contractResolver = null)
        {
            if (typeof(T) == typeof(string)) return json is T ? (T)(object)json : default;
            return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings()
            {
                ContractResolver = contractResolver ?? TrickContractResolver,
                Converters = Converters,
                NullValueHandling = OverrideNullValueHandling ?? (includeNull ? NullValueHandling.Include : NullValueHandling.Ignore),
                DefaultValueHandling = OverrideDefaultValueHandling ?? DefaultValueHandling.Ignore,
                ReferenceLoopHandling = OverrideReferenceLoopHandling ?? ReferenceLoopHandling.Ignore
            });
        }

        /// <summary>
        /// Deserialize the json to an object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json">The object</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static T DeserializeJson<T>(this object json, IContractResolver contractResolver = null)
        {
            return DeserializeJson<T>(json, true, contractResolver);
        }

        /// <summary>
        /// Deserialize an to a json object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object</param>
        /// <param name="includeNull">True if we include null fields</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static T DeserializeJson<T>(this object obj, bool includeNull, IContractResolver contractResolver = null)
        {
            if (obj is string s) return DeserializeJson<T>(s, includeNull, contractResolver);
            if (typeof(T) == typeof(string)) return obj is T t ? t : obj?.ToString() is T str ? str : default;
            return JsonConvert.DeserializeObject<T>(SerializeToJson(obj, false, contractResolver), new JsonSerializerSettings()
            {
                ContractResolver = contractResolver ?? TrickContractResolver,
                Converters = Converters,
                NullValueHandling = OverrideNullValueHandling ?? (includeNull ? NullValueHandling.Include : NullValueHandling.Ignore),
                DefaultValueHandling = OverrideDefaultValueHandling ?? DefaultValueHandling.Ignore,
                ReferenceLoopHandling = OverrideReferenceLoopHandling ?? ReferenceLoopHandling.Ignore
            });
        }

        /// <summary>
        /// Deserialize the json string to a json object
        /// </summary>
        /// <param name="json">The json string</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static object DeserializeJson(this string json, Type type, IContractResolver contractResolver = null)
        {
            return DeserializeJson(json, type, true, contractResolver);
        }

        /// <summary>
        /// Deserialize the json string to a json object
        /// </summary>
        /// <param name="json">The json string</param>
        /// <param name="type">The type</param>
        /// <param name="includeNull">True if we include null fields</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static object DeserializeJson(this string json, Type type, bool includeNull, IContractResolver contractResolver = null)
        {
            if (type == typeof(string)) return json;
            return JsonConvert.DeserializeObject(json, type, new JsonSerializerSettings()
            {
                ContractResolver = contractResolver ?? TrickContractResolver,
                Converters = Converters,
                NullValueHandling = OverrideNullValueHandling ?? (includeNull ? NullValueHandling.Include : NullValueHandling.Ignore),
                DefaultValueHandling = OverrideDefaultValueHandling ?? DefaultValueHandling.Ignore,
                ReferenceLoopHandling = OverrideReferenceLoopHandling ?? ReferenceLoopHandling.Ignore
            });
        }

        /// <summary>
        /// Deserialize the json to an object
        /// </summary>
        /// <param name="json">The object</param>
        /// <param name="type">The type</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static object DeserializeJson(this object json, Type type, IContractResolver contractResolver = null)
        {
            return DeserializeJson(json, type, true, contractResolver);
        }

        /// <summary>
        /// Deserialize an to a json object
        /// </summary>
        /// <param name="obj">The object</param>
        /// <param name="type">The type</param>
        /// <param name="includeNull">True if we include null fields</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static object DeserializeJson(this object obj, Type type, bool includeNull, IContractResolver contractResolver = null)
        {
            string json;
            if (obj is string s)
            {
                json = s;
            }
            else
            {
                json = SerializeToJson(obj, false);
            }

            if (type == typeof(string)) return json;
            return JsonConvert.DeserializeObject(json, type, new JsonSerializerSettings()
            {
                ContractResolver = contractResolver ?? TrickContractResolver,
                Converters = Converters,
                NullValueHandling = OverrideNullValueHandling ?? (includeNull ? NullValueHandling.Include : NullValueHandling.Ignore),
                DefaultValueHandling = OverrideDefaultValueHandling ?? DefaultValueHandling.Ignore,
                ReferenceLoopHandling = OverrideReferenceLoopHandling ?? ReferenceLoopHandling.Ignore
            });
        }


        /// <summary>
        /// Serialize an object to json string
        /// </summary>
        /// <param name="obj">The object</param>
        /// <param name="indented">Pretty print</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static string SerializeToJson(this object obj, bool indented, IContractResolver contractResolver = null)
        {
            return SerializeToJson(obj, indented, true, contractResolver);
        }
        
        /// <summary>
        /// Serialize an object to json string and catch any exceptions
        /// </summary>
        /// <param name="obj">The object</param>
        /// <param name="indented">Pretty print</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static string SerializeToJsonTryCatch(this object obj, bool indented, IContractResolver contractResolver = null)
        {
            try
            {
                return SerializeToJson(obj, indented, true, contractResolver);
            }
            catch (Exception ex)
            {
                Logger.Game.LogException(ex);
                return default;
            }
        }

        /// <summary>
        /// Serialize an object to json string
        /// </summary>
        /// <param name="obj">The object</param>
        /// <param name="indented">Pretty print</param>
        /// <param name="includeNull">True if we include null fields</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static string SerializeToJson(this object obj, bool indented, bool includeNull, IContractResolver contractResolver = null)
        {
            return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None, new JsonSerializerSettings()
            {
                ContractResolver = contractResolver ?? TrickContractResolver,
                Converters = Converters,
                NullValueHandling = OverrideNullValueHandling ?? (includeNull ? NullValueHandling.Include : NullValueHandling.Ignore),
                DefaultValueHandling = OverrideDefaultValueHandling ?? DefaultValueHandling.Ignore,
                ReferenceLoopHandling = OverrideReferenceLoopHandling ?? ReferenceLoopHandling.Ignore
            });
        }
        
        /// <summary>
        /// Serialize a json object to base64 format
        /// </summary>
        /// <param name="obj">The object</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static string SerializeToJsonBase64(this object obj, IContractResolver contractResolver = null)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(obj.SerializeToJson(false, false, contractResolver)));
        }

        /// <summary>
        /// Deserialize a base64 string to a json object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="base64Str">The base64 string</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static T DeserializeJsonBase64<T>(this string base64Str, IContractResolver contractResolver = null)
        {
            return DeserializeJson<T>(Encoding.UTF8.GetString(Convert.FromBase64String(base64Str)), contractResolver);
        }
        

        /// <summary>
        /// Deserialize a base64 string to a json object and catch any exceptions
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="base64Str">The base64 string</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static T DeserializeJsonBase64TryCatch<T>(this string base64Str, IContractResolver contractResolver = null)
        {
            try
            {
                return DeserializeJson<T>(Encoding.UTF8.GetString(Convert.FromBase64String(base64Str)), contractResolver);
            }
            catch (Exception ex)
            {
                Logger.Game.LogException(ex);
                return default;
            }
        }

        /// <summary>
        /// Deserialize a base64 string to a json object
        /// </summary>
        /// <param name="base64Str">The base64 string</param>
        /// <param name="type">The type</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static object DeserializeJsonBase64(this string base64Str, Type type, IContractResolver contractResolver = null)
        {
            return DeserializeJson(Encoding.UTF8.GetString(Convert.FromBase64String(base64Str)), type, contractResolver);
        }
        
        /// <summary>
        /// Deserialize a base64 string to a json object and catch any exceptions
        /// </summary>
        /// <param name="base64Str">The base64 string</param>
        /// <param name="type">The type</param>
        /// <param name="contractResolver">Custom contract resolver</param>
        /// <returns></returns>
        public static object DeserializeJsonBase64TryCatch(this string base64Str, Type type, IContractResolver contractResolver = null)
        {
            try
            {
                return DeserializeJson(Encoding.UTF8.GetString(Convert.FromBase64String(base64Str)), type, contractResolver);
            }
            catch (Exception ex)
            {
                Logger.Game.LogException(ex);
                return default;
            }
        }

        public static string JsonPrettify(this string json)
        {
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter())
            {
                var jsonReader = new JsonTextReader(stringReader);
                var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.None };
                jsonWriter.WriteToken(jsonReader);
                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Test if two json are the same
        /// </summary>
        /// <param name="expectedJson"></param>
        /// <param name="targetJson"></param>
        /// <param name="strictTest"></param>
        /// <returns></returns>
        public static bool IsSameJson(this string expectedJson, string targetJson, bool strictTest = false)
        {
            if (expectedJson == null && targetJson == null) return true;
            if (expectedJson == null) return false;
            if (targetJson == null) return false;

            // same length, just compare them. Most of the time this is sufficient, but can be wrong.
            if (!strictTest && expectedJson.Length == targetJson.Length)
            {
                return string.Equals(expectedJson, targetJson, StringComparison.InvariantCulture);
            }

            // prettify both, so they should be equal
            string p1 = JsonPrettify(expectedJson);
            string p2 = JsonPrettify(targetJson);

            return string.Equals(p1, p2, StringComparison.InvariantCulture);
        }

        /// <summary>
        /// Test if two json are the same
        /// </summary>
        /// <param name="expectedJson"></param>
        /// <param name="targetJson"></param>
        /// <returns></returns>
        public static bool IsSameJson<T>(this string expectedJson, string targetJson)
        {
            if (expectedJson == null && targetJson == null) return true;
            if (expectedJson == null) return false;
            if (targetJson == null) return false;

            var instanceA = expectedJson.DeserializeJson<T>();
            var instanceB = targetJson.DeserializeJson<T>();

            return Equals(instanceA, instanceB);
        }
        
        public static T DifferenceObject<T>(this object a, object b)
        {
            return DifferenceObjectJsonV2(a, b).DeserializeJson<T>();
        }

        public static string DifferenceObjectJsonV2(this object a, object b)
        {
            var modelJson = a is string jsonA ? jsonA : a.SerializeToJson(false, false);
            var targetJson = b is string jsonB ? jsonB : b.SerializeToJson(false, false);
            return FindDiffV2(JToken.Parse(targetJson), JToken.Parse(modelJson)).ToString(Formatting.None);
        }

        public static JToken FindDiffV2(this JToken currentToken, JToken modelToken)
        {
            JToken diff = new JObject();
            if (JToken.DeepEquals(currentToken, modelToken)) return diff;

            switch (currentToken.Type)
            {
                case JTokenType.Object:
                {
                    if (!(currentToken is JObject current) || !(modelToken is JObject model)) return diff;
                    var addedKeys = current.Properties().Select(c => c.Name)
                        .Except(model.Properties().Select(c => c.Name)).ToList();
                    /*var removedKeys = model.Properties().Select(c => c.Name)
                        .Except(current.Properties().Select(c => c.Name)).ToList();*/
                    var unchangedKeys = current.Properties().Where(c => JToken.DeepEquals(c.Value, modelToken[c.Name]))
                        .Select(c => c.Name);
                    
                    foreach (var k in addedKeys)
                    {
                        diff[k] = currentToken[k];
                    }
					
                    var potentiallyModifiedKeys =
                        current.Properties().Select(c => c.Name).Except(addedKeys).Except(unchangedKeys);
                    foreach (var k in potentiallyModifiedKeys)
                    {
						diff[k] = FindDiffV2(current[k], model[k]);
                    }
                }
                    break;
                case JTokenType.Array:
                    return currentToken;
                default:
					return currentToken;
            }

            return diff;
        }

        public static JObject FindDiff(this JToken currentToken, JToken modelToken)
        {
            var diff = new JObject();
            if (JToken.DeepEquals(currentToken, modelToken)) return diff;

            switch (currentToken.Type)
            {
                case JTokenType.Object:
                {
                    var current = currentToken as JObject;
                    var model = modelToken as JObject;
                    var addedKeys = current.Properties().Select(c => c.Name)
                        .Except(model.Properties().Select(c => c.Name));
                    var removedKeys = model.Properties().Select(c => c.Name)
                        .Except(current.Properties().Select(c => c.Name));
                    var unchangedKeys = current.Properties().Where(c => JToken.DeepEquals(c.Value, modelToken[c.Name]))
                        .Select(c => c.Name);
                    
                    foreach (var k in addedKeys)
                    {
                        diff[k] = new JObject
                        {
                            ["+"] = currentToken[k]
                        };
                    }
                    foreach (var k in removedKeys)
                    {
                        diff[k] = new JObject
                        {
                            ["-"] = modelToken[k]
                        };
                    }

                    var potentiallyModifiedKeys =
                        current.Properties().Select(c => c.Name).Except(addedKeys).Except(unchangedKeys);
                    foreach (var k in potentiallyModifiedKeys)
                    {
                        var foundDiff = FindDiff(current[k], model[k]);
                        if (foundDiff.HasValues) diff[k] = foundDiff;
                    }
                }
                    break;
                case JTokenType.Array:
                {
                    var current = currentToken as JArray;
                    var model = modelToken as JArray;
                    var plus = new JArray(current.Except(model, new JTokenEqualityComparer()));
                    var minus = new JArray(model.Except(current, new JTokenEqualityComparer()));
                    if (plus.HasValues) diff["+"] = plus;
                    if (minus.HasValues) diff["-"] = minus;
                }
                    break;
                default:
                    diff["+"] = currentToken;
                    diff["-"] = modelToken;
                    break;
            }

            return diff;
        }

        public static string DifferenceObjectJson(this object a, object b)
        {
            var jsonA = a is string jA ? jA : a.SerializeToJson(false, false);
            var jsonB = b is string jB ? jB : b.SerializeToJson(false, false);
            var dictA = (jsonA.DeserializeJson<Dictionary<string, object>>() ?? new Dictionary<string, object>()).OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value);
            var dictB = (jsonB.DeserializeJson<Dictionary<string, object>>() ?? new Dictionary<string, object>()).OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value);
            var diff = dictB.Except(dictA).OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            List<string> duplicateKeys = new List<string>();

            foreach (var p in diff)
            {
                if (p.Value is JArray && dictA.ContainsKey(p.Key) && dictB.ContainsKey(p.Key) && dictA[p.Key] is JArray j1 && dictB[p.Key] is JArray j2 && JToken.DeepEquals(j1, j2))
                {
                    duplicateKeys.Add(p.Key);
                }

                if (p.Value is JObject && dictA.ContainsKey(p.Key) && dictB.ContainsKey(p.Key) && dictA[p.Key] is JObject jo1 && dictB[p.Key] is JObject jo2 && JToken.DeepEquals(jo1, jo2))
                {
                    duplicateKeys.Add(p.Key);
                }
            }

            duplicateKeys.ForEach(s =>
            {
                if (diff.ContainsKey(s)) diff.Remove(s);
            });
            
            string diffJson = diff.SerializeToJson(false, false);
            return diffJson;
        }
    }
}
