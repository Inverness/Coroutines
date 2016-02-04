using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using Coroutines.Framework;
using Coroutines.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

            var thread = exc.StartThread(t.DelaySeconds(1));

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

            var thread = exc.StartThread(t.DelaySeconds(1));

            Assert.True(thread.Status == CoroutineThreadStatus.Yielded);

            exc.Tick(TimeSpan.FromSeconds(0.55));

            Assert.True(thread.Status == CoroutineThreadStatus.Yielded);

            exc.Tick(TimeSpan.FromSeconds(0.55));

            Assert.True(thread.Status == CoroutineThreadStatus.Yielded);

            var serializer = new JsonSerializer();
            serializer.Converters.Add(new CoroutineConverter());

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Converters = new JsonConverter[] {new CoroutineConverter(), new FromStringConverter(),  },
                TypeNameHandling = TypeNameHandling.Auto
            };

            string resultString = JsonConvert.SerializeObject(exc, settings);

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

        public override bool CanConvert(Type objectType)
        {
            return s_coroutineTypeInfo.IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IteratorState state = _isc.ToState((IEnumerator<CoroutineAction>) value);
            serializer.Serialize(writer, state);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var state = serializer.Deserialize<IteratorState>(reader);

            var result = (IEnumerator<CoroutineAction>) _isc.FromState(state);

            return result;
        }
    }

    public class FromStringConverter : JsonConverter
    {
        private static readonly Dictionary<Type, Func<string, object>> s_types =
            new Dictionary<Type, Func<string, object>>
            {
                {typeof (TimeSpan), s => TimeSpan.Parse(s)},
                {typeof (Guid), s => Guid.Parse(s)}
            };

        public override bool CanConvert(Type objectType)
        {
            return s_types.ContainsKey(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("$type");
            writer.WriteValue(value.GetType().FullName);
            writer.WritePropertyName("$value");
            writer.WriteValue(value);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type type, object value, JsonSerializer serializer)
        {
            reader.Read();
            if (reader.TokenType != JsonToken.PropertyName || (string) reader.Value != "$type")
                throw new JsonSerializationException("expected $type");

            reader.Read();

            reader.Read();
            if (reader.TokenType != JsonToken.PropertyName || (string) reader.Value != "$value")
                throw new JsonSerializationException("expected $value");

            reader.Read();

            string s = (string) reader.Value;

            reader.Read();

            return s_types[type](s);
        }
    }
}
