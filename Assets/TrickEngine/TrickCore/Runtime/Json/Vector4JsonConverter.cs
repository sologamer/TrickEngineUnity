using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace TrickCore
{
    [Preserve]
    public sealed class Vector4JsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof (Vector4).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var readerValue = (string) reader.Value;
            if (readerValue == null)
            {
                return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>)
                    ? (object) null
                    : Vector4.zero;
            }
            
            // Load the data we're looking at from the stream
            var values = ((string)reader.Value)
                // Split the data, accept both ',' and ' ' as separators
                .Split(new []{',', ' '}, StringSplitOptions.RemoveEmptyEntries)
                // Parse the separate bits into floats (do not remove the template parameters, Unity's fault)
                .Select<string, int>(f => int.Parse(f, CultureInfo.InvariantCulture)).ToArray();

            // Stuff the data into a Vector3
            return new Vector4(values[0], values[1], values[2], values[3]);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Write a simple representation of our value to the string
            var vector = (Vector4)value;
            writer.WriteValue(
                vector.x.ToString(CultureInfo.InvariantCulture) + "," +
                vector.y.ToString(CultureInfo.InvariantCulture) + "," +
                vector.z.ToString(CultureInfo.InvariantCulture) + "," +
                vector.w.ToString(CultureInfo.InvariantCulture));
        }
    }
}