using System;
using System.Collections.Generic;

namespace Coroutines.Framework
{
    /// <summary>
    /// An action that delays execution of the current thread for an amount of time.
    /// </summary>
    public sealed class DelayAction : CoroutineAction
    {
        public DelayAction(TimeSpan time)
        {
            if (time.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(time));
            Time = time;
        }

        public TimeSpan Time { get; }

        public override IEnumerable<CoroutineAction> GetNext(CoroutineThread thread)
        {
            return thread.Executor.Delay(Time);
        }
    }
}