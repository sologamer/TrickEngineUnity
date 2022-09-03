using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using TrickCore;
using UnityEngine;

namespace TrickCore
{
    [Preserve]
    public class FirebaseTimestampJsonConverter : IsoDateTimeConverter
    {
        public override bool CanConvert(Type objectType)
        {
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
        if (typeof (Firebase.Firestore.Timestamp).IsAssignableFrom(objectType)) return true;
#endif
            return base.CanConvert(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
        if (value is Firebase.Firestore.Timestamp timestamp)
        {
            base.WriteJson(writer, timestamp.ToDateTime(), serializer);
            return;
        }
#endif  
            base.WriteJson(writer, value, serializer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
        if (existingValue is Firebase.Firestore.Timestamp timestamp)
            return base.ReadJson(reader, objectType, timestamp.ToDateTime(), serializer);
#endif
            if (reader.TokenType == JsonToken.StartObject)
            {
                var token = JToken.Load(reader);
                if (token.HasValues)
                {
                    DateTime dateTime = s_unixEpoch;
                    dateTime = dateTime.AddSeconds(token.Value<long>("_seconds"));
                    return dateTime.AddTicks((long) (token.Value<long>("_nanoseconds") / 100));
                }
            }
            
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
        
        private static readonly DateTime s_unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}