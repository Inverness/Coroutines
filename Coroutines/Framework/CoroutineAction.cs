using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Coroutines.Framework
{
    /// <summary>
    /// Describes an action that can occurr after a coroutine yields.
    /// </summary>
    public abstract class CoroutineAction
    {
        private static readonly object s_trueBox = true;
        private static readonly object s_falseBox = false;

        /// <summary>
        /// Gets the next coroutine to be pushed onto the thread's stack. If null, yield until the next tick.
        /// </summary>
        /// <param name="thread">The current thread.</param>
        /// <param name="cor">A coroutine to push if the behavior is Push.</param>
        /// <returns>The behavior of the action.</returns>
        public abstract CoroutineActionBehavior Process(CoroutineThread thread, ref IEnumerable cor);

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
        /// Create an action that delays execution for an amount of time.
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