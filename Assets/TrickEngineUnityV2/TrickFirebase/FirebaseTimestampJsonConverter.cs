using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TrickCore;

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
        
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }
}