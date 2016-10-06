using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace Coroutines.Serialization.Json
{
    public class NameValueDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (Dictionary<string, object>);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            foreach (KeyValuePair<string, object> item in (Dictionary<string, object>) value)
            {
                writer.WritePropertyName(item.Key);
                WriteObject(writer, item.Value, serializer);
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            VerifyTokenType(reader, JsonToken.StartObject);

            var dict = (Dictionary<string, object>) existingValue ?? new Dictionary<string, object>();

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var name = (string) reader.Value;

                reader.Read();

                object value = ReadObject(reader, serializer);

                dict[name] = value;
            }

            return dict;
        }

        private static void WriteObject(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            Type valueType = value.GetType();
            TypeInfo valueTypeInfo = valueType.GetTypeInfo();

            writer.WriteStartObject();

            writer.WritePropertyName("$type");
            writer.WriteValue(NameUtility.GetSimpleAssemblyQualifiedName(valueType));

            writer.WritePropertyName("$value");
            
            if (valueTypeInfo.IsPrimitive)
                writer.WriteValue(value);
            else
                serializer.Serialize(writer, value);

            writer.WriteEndObject();
        }

        private static object ReadObject(JsonReader reader, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            VerifyTokenType(reader, JsonToken.StartObject);

            reader.Read();
            VerifyTokenType(reader, JsonToken.PropertyName);
            VerifyValue(reader, "$type");

            reader.Read();
            VerifyTokenType(reader, JsonToken.String);

            Type valueType = Type.GetType((string) reader.Value);
            TypeInfo valueTypeInfo = valueType.GetTypeInfo();

            reader.Read();
            VerifyTokenType(reader, JsonToken.PropertyName);
            VerifyValue(reader, "$value");

            reader.Read();

            object result;
            if (valueTypeInfo.IsPrimitive)
            {
                result = reader.ValueType == valueType ? reader.Value : Convert.ChangeType(reader.Value, valueType);
            }
            else
            {
                result = serializer.Deserialize(reader, valueType);
            }

            reader.Read();
            VerifyTokenType(reader, JsonToken.EndObject);

            return result;
        }

        private static void VerifyTokenType(JsonReader reader, JsonToken type)
        {
            if (reader.TokenType != type)
                throw new JsonSerializationException("expected " + type);
        }

        private static void VerifyValue(JsonReader reader, string value)
        {
            if (reader.Value as string != value)
                throw new JsonSerializationException("expected value " + value);
        }
    }
}