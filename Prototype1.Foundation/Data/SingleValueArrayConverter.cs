using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Prototype1.Foundation.Data
{
    public class SingleValueArrayConverter<T> : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var retVal = new Object();
            if (reader.TokenType == JsonToken.StartObject)
            {
                var instance = (T)serializer.Deserialize(reader, typeof(T));
                retVal = new List<T> { instance };
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                retVal = serializer.Deserialize(reader, objectType);
            }
            return retVal;
        }

        public override bool CanConvert(Type objectType)
        {
            return false;
        }
    }
}