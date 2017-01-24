using System.Collections;

namespace Coroutines
{
    /// <summary>
    /// Provides generic tools for working with coroutines.
    /// </summary>
    public static class Coroutine
    {
        private static IEnumerator s_noopEnumerator;
        private static IEnumerable s_noopEnumerable;

        /// <summary>
        /// Gets an instance of a no-op coroutine.
        /// </summary>
        public static IEnumerable NoOp
        {
            get
            {
                IEnumerable e = s_noopEnumerable;
                if (e == null)
                {
                    s_noopEnumerable = e = new ImplEnumerable();
                    s_noopEnumerator = new ImplEnumerator();
                }
                return e;
            }
        }

        private class ImplEnumerable : IEnumerable
        {
            public IEnumerator GetEnumerator()
            {
                return s_noopEnumerator;
            }
        }

        private class ImplEnumerator : IEnumerator
        {
            public bool MoveNext()
            {
                return false;
            }

            public void Reset()
            {
            }

            // ReSharper disable once UnassignedGetOnlyAutoProperty
            public object Current { get; }
        }
    }
}
