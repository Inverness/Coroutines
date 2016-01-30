using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace CoroutineSerialization.Tests
{
    public class IteratorTests
    {
        private readonly ITestOutputHelper _out;

        public IteratorTests(ITestOutputHelper output)
        {
            _out = output;
        }

        [Fact]
        public void StaticYieldOnly()
        {
            var serializer = new IteratorStateConverter();

            IEnumerator<int> iterator = TestClass.StaticYieldOnly().GetEnumerator();

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

            IEnumerator<int> iterator = TestClass.StaticYieldWithVar().GetEnumerator();

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

            IEnumerator<int> iterator = TestClass.StaticYieldWithVarAndArg(5).GetEnumerator();

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

    public class TestClass
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
    }
}
