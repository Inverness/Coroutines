using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using Coroutines.Framework;
using Coroutines.Serialization;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using static Coroutines.Framework.CoroutineAction;

namespace Coroutines.Tests
{
    public class CoroutineSerializationTests
    {
        private readonly ITestOutputHelper _output;

        public CoroutineSerializationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DelaySecondsNoSerialization()
        {
            var t = new CoroutineTestClass(_output);

            var exc = new CoroutineExecutor();

            var thread = exc.Start(t.DelaySeconds(1));

            Assert.True(thread.Status == CoroutineThreadStatus.Yielded);

            exc.Tick(TimeSpan.FromSeconds(0.55));

            Assert.True(thread.Status == CoroutineThreadStatus.Yielded);

            exc.Tick(TimeSpan.FromSeconds(0.55));

            Assert.True(thread.Status == CoroutineThreadStatus.Yielded);

            exc.Tick(TimeSpan.FromSeconds(0.55));

            Assert.True(thread.Status == CoroutineThreadStatus.Finished);
        }

        [Fact]
        public void DelaySecondsWithSerialization()
        {
            var t = new CoroutineTestClass(_output);

            var exc = new CoroutineExecutor();

            var thread = exc.Start(t.DelaySeconds(1));

            Assert.True(thread.Status == CoroutineThreadStatus.Yielded);

            exc.Tick(TimeSpan.FromSeconds(0.55));

            Assert.True(thread.Status == CoroutineThreadStatus.Yielded);

            exc.Tick(TimeSpan.FromSeconds(0.55));

            Assert.True(thread.Status == CoroutineThreadStatus.Yielded);

            var serializer = new JsonSerializer();
            serializer.Converters.Add(new CoroutineConverter(true, false));

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters = new JsonConverter[] { new CoroutineConverter(true, false), new NameValueDictionaryConverter() },
                TypeNameHandling = TypeNameHandling.Auto
            };

            string resultString = JsonConvert.SerializeObject(exc, settings);

            //var dcss = new DataContractSerializerSettings {PreserveObjectReferences = true};
            //var dcs = new DataContractSerializer(typeof(CoroutineExecutor), dcss);
            //var ms = new MemoryStream();
            //dcs.WriteObject(ms, exc);

            //var dat = ms.ToArray();
            //var mss = Encoding.UTF8.GetString(dat, 0, dat.Length);

            exc.Tick(TimeSpan.FromSeconds(0.55));

            Assert.True(thread.Status == CoroutineThreadStatus.Finished);


            var desexc = JsonConvert.DeserializeObject<CoroutineExecutor>(resultString, settings);
        }
    }

    [DataContract]
    public class CoroutineTestClass
    {
        private readonly ITestOutputHelper _output;

        public CoroutineTestClass(ITestOutputHelper output)
        {
            _output = output;
        }

        public IEnumerable<CoroutineAction> DelaySeconds(double seconds)
        {
            _output.WriteLine("Begin");
            yield return Delay(seconds);
            _output.WriteLine("End");
        } 
    }

    public class CoroutineConverter : JsonConverter
    {
        private static readonly TypeInfo s_coroutineTypeInfo = typeof (IEnumerator<CoroutineAction>).GetTypeInfo();
        private readonly IteratorStateConverter _isc = new IteratorStateConverter();

        private readonly bool _withThis;
        private readonly bool _withCurrent;

        public CoroutineConverter(bool withThis, bool withCurrent)
        {
            _withThis = withThis;
            _withCurrent = withCurrent;
        }

        public override bool CanConvert(Type objectType)
        {
            return s_coroutineTypeInfo.IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IteratorState state = _isc.ToState((IEnumerator<CoroutineAction>) value);

            Exclude(state);

            serializer.Serialize(writer, state);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var state = serializer.Deserialize<IteratorState>(reader);

            Exclude(state);

            var result = (IEnumerator<CoroutineAction>) _isc.FromState(state);

            return result;
        }

        private void Exclude(IteratorState state)
        {
            if (!_withThis)
                state.This = null;
            if (!_withCurrent)
                state.Current = null;
        }
    }

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

    public sealed class PrimitiveJsonConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetTypeInfo().IsPrimitive;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            switch (serializer.TypeNameHandling)
            {
                case TypeNameHandling.All:
                    writer.WriteStartObject();
                    writer.WritePropertyName("$type", false);

                    switch (serializer.TypeNameAssemblyFormat)
                    {
                        case FormatterAssemblyStyle.Full:
                            writer.WriteValue(value.GetType().AssemblyQualifiedName);
                            break;
                        default:
                            writer.WriteValue(value.GetType().FullName);
                            break;
                    }

                    writer.WritePropertyName("$value", false);
                    writer.WriteValue(value);
                    writer.WriteEndObject();
                    break;
                default:
                    writer.WriteValue(value);
                    break;
            }
        }
    }
}
