using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Coroutines.Framework
{
    /// <summary>
    /// Contains helper methods to create the standard actions. It's recommend to statically import this class when
    /// writing coroutines.
    /// </summary>
    public static class StandardActions
    {
        private static readonly object s_trueBox = true;
        private static readonly object s_falseBox = false;

        /// <summary>
        /// Create an action that executes coroutines in parallel, returning when either all or finished or one has
        /// faulted.
        /// </summary>
        public static ParallelAction Parallel(IEnumerable<IEnumerable> enumerables)
        {
            if (enumerables == null)
                throw new ArgumentNullException(nameof(enumerables));
            return new ParallelAction(enumerables.ToArray());
        }

        /// <summary>
        /// Create an action that executes coroutines in parallel, returning when either all or finished or one has
        /// faulted.
        /// </summary>
        public static ParallelAction Parallel(params IEnumerable[] enumerables)
        {
            return new ParallelAction(enumerables);
        }

        /// <summary>
        /// Create an action that delays execution for a number of seconds.
        /// </summary>
        public static DelayAction Delay(double seconds)
        {
            return new DelayAction(TimeSpan.FromSeconds(seconds));
        }

        /// <summary>
        /// Create an action that delays execution for an amount of time.
        /// </summary>
        public static DelayAction Delay(TimeSpan time)
        {
            return new DelayAction(time);
        }

        /// <summary>
        /// Create an action that sets the current thread result.
        /// </summary>
        /// <param name="result">The result</param>
        public static ResultAction Result(bool result)
        {
            return new ResultAction(result ? s_trueBox : s_falseBox);
        }

        /// <summary>
        /// Create an action that sets the current thread result.
        /// </summary>
        /// <param name="result">The result</param>
        public static ResultAction Result(object result)
        {
            return new ResultAction(result);
        }
    }
}