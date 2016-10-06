using System;
using System.Collections;

namespace Coroutines
{
    /// <summary>
    /// An action that delays execution of the current thread for an amount of time.
    /// </summary>
    public class DelayAction : CoroutineAction
    {
        private TimeSpan _time;

        public DelayAction(TimeSpan time)
        {
            if (time.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(time));
            _time = time;
        }

        // ReSharper disable once RedundantAssignment
        public override CoroutineActionBehavior Process(CoroutineThread thread, ref IEnumerable cor)
        {
            cor = thread.Executor.Delay(_time);
            _time = default(TimeSpan);
            return CoroutineActionBehavior.Push;
        }
    }
}