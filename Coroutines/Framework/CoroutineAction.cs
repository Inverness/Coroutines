using System.Collections;

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
        /// <param name="thread">The current thread.</param>
        /// <param name="cor">A coroutine to push if the behavior is Push.</param>
        /// <returns>The behavior of the action.</returns>
        public abstract CoroutineActionBehavior Process(CoroutineThread thread, ref IEnumerable cor);
    }
}