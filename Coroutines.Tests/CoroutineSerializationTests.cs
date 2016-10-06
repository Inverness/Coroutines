using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Coroutines.Framework;
using Coroutines.Serialization.Json;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using static Coroutines.Framework.StandardActions;

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
}
