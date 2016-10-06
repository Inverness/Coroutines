using System;
using System.Collections;
using System.Reflection;
using Newtonsoft.Json;

namespace Coroutines.Serialization.Json
{
    public class CoroutineConverter : JsonConverter
    {
        private static readonly TypeInfo s_coroutineTypeInfo = typeof (IEnumerator).GetTypeInfo();
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
            IteratorState state = _isc.ToState((IEnumerator) value);

            Exclude(state);

            serializer.Serialize(writer, state);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var state = serializer.Deserialize<IteratorState>(reader);

            Exclude(state);

            IEnumerator result = _isc.FromState(state);

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
}
