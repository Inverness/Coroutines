using System;
using System.Collections;

namespace Coroutines.Framework
{
    /// <summary>
    /// An action that delays execution of the current thread for an amount of time.
    /// </summary>
    public class DelayAction : CoroutineAction
    {
        public DelayAction(TimeSpan time)
        {
            if (time.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(time));
            Time = time;
        }

        public TimeSpan Time { get; }

        public override CoroutineActionBehavior Process(CoroutineThread thread, ref IEnumerable cor)
        {
            cor = thread.Executor.Delay(Time);
            return CoroutineActionBehavior.Push;
        }
    }
}