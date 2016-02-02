using System;
using System.Collections.Generic;
using Coroutines.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace Coroutines.Tests
{
    public class IteratorSerializationTests
    {
        private readonly ITestOutputHelper _out;

        public IteratorSerializationTests(ITestOutputHelper output)
        {
            _out = output;
        }

        [Fact]
        public void StaticYieldOnly()
        {
            var serializer = new IteratorStateConverter();

            IEnumerator<int> iterator = SerializationCoroutines.StaticYieldOnly().GetEnumerator();

            iterator.MoveNext();

            IteratorState state1 = serializer.ToState(iterator);

            Assert.True((int) state1.Current == 1);

            iterator.MoveNext();

            IteratorState state2 = serializer.ToState(iterator);

            Assert.True((int) state2.Current == 2);
        }

        [Fact]
        public void StaticYieldWithVar()
        {
            var serializer = new IteratorStateConverter();

            IEnumerator<int> iterator = SerializationCoroutines.StaticYieldWithVar().GetEnumerator();

            iterator.MoveNext();

            IteratorState state1 = serializer.ToState(iterator);

            Assert.True((int) state1.Current == 1);

            iterator.MoveNext();

            IteratorState state2 = serializer.ToState(iterator);

            Assert.True((int) state2.Current == 3);

            var newIterator = (IEnumerator<int>) serializer.FromState(state2);
            
            Assert.True(newIterator.Current == 3);
        }

        [Fact]
        public void StaticYieldWithVarAndArg()
        {
            var serializer = new IteratorStateConverter();

            IEnumerator<int> iterator = SerializationCoroutines.StaticYieldWithVarAndArg(5).GetEnumerator();

            iterator.MoveNext();

            IteratorState state1 = serializer.ToState(iterator);

            Assert.True((int) state1.Current == 5);

            iterator.MoveNext();

            IteratorState state2 = serializer.ToState(iterator);

            Assert.True((int) state2.Current == 15);

            var newIterator = (IEnumerator<int>) serializer.FromState(state2);

            Assert.True(newIterator.Current == 15);
        }
    }

    public class SerializationCoroutines
    {
        internal static IEnumerable<int> StaticYieldOnly()
        {
            yield return 1;
            yield return 2;
        }

        internal static IEnumerable<int> StaticYieldWithVar()
        {
            int r = 1;
            yield return r;
            r *= 3;
            yield return r;
            r *= 4;
            yield return r;
        }

        internal static IEnumerable<int> StaticYieldWithVarAndArg(int start)
        {
            int r = start;
            yield return r;
            r *= 3;
            yield return r;
            r *= 4;
            yield return r;
        }

        internal static IEnumerable<int> StaticYieldWithVarAndArgAndCatch(int start)
        {
            int r = 0;
            try
            {
                r = start;
                yield return r;
                r = Mult(r, 3);
                yield return r;
                r = Mult(r, 4);
                yield return r;
            }
            finally
            {
                r = start * 99;
            }

        }

        private static int Mult(int a, int f)
        {
            return a * f;
        }
    }
}
