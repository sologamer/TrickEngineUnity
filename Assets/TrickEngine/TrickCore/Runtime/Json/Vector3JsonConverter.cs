using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace TrickCore
{
    [Preserve]
    public sealed class Vector3JsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof (Vector3).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var readerValue = (string) reader.Value;
            if (readerValue == null)
            {
                return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>)
                    ? (object) null
                    : Vector3.zero;
            }
            
            // Load the data we're looking at from the stream
            var values = readerValue
                // Split the data, accept both ',' and ' ' as separators
                .Split(new []{',', ' '}, StringSplitOptions.RemoveEmptyEntries)
                // Parse the separate bits into floats (do not remove the template parameters, Unity's fault)
                .Select<string, float>(f => float.Parse(f, CultureInfo.InvariantCulture)).ToArray();

            // Stuff the data into a Vector3
            return new Vector3(values[0], values[1], values[2]);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Write a simple representation of our value to the string
            var vector = (Vector3)value;
            writer.WriteValue(
                vector.x.ToString(CultureInfo.InvariantCulture) + "," +
                vector.y.ToString(CultureInfo.InvariantCulture) + "," +
                vector.z.ToString(CultureInfo.InvariantCulture));
        }
    }
}