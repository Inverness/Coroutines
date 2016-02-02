using System;
using System.Collections.Generic;
using System.Linq;

namespace Coroutines.Framework
{
    /// <summary>
    /// Describes an action that can occurr after a coroutine yields.
    /// </summary>
    public abstract class CoroutineAction
    {
        /// <summary>
        /// Gets the next coroutine to be pushed onto the thread's stack. If null, yield until the next tick.
        /// </summary>
        /// <param name="thread">The curren thread.</param>
        /// <returns>A coroutine that will be pushed onto the stack, or null to yield until the next tick.</returns>
        public abstract IEnumerable<CoroutineAction> GetNext(CoroutineThread thread);

        /// <summary>
        /// Create an action that executes coroutines in parallel, returning when either all or finished or one has
        /// faulted.
        /// </summary>
        public static ParallelAction Parallel(IEnumerable<IEnumerable<CoroutineAction>> enumerables)
        {
            if (enumerables == null)
                throw new ArgumentNullException(nameof(enumerables));
            return new ParallelAction(enumerables.ToArray());
        }

        /// <summary>
        /// Create an action that executes coroutines in parallel, returning when either all or finished or one has
        /// faulted.
        /// </summary>
        public static ParallelAction Parallel(params IEnumerable<CoroutineAction>[] enumerables)
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
        /// Create an action that executes a coroutine on the same thread.
        /// </summary>
        public static ExecuteAction Execute(IEnumerable<CoroutineAction> coroutine)
        {
            return new ExecuteAction(coroutine);
        }
    }
}