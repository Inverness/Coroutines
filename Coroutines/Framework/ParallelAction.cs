using System;
using System.Collections;

namespace Coroutines.Framework
{
    /// <summary>
    /// An action that executes coroutines in parallel.
    /// </summary>
    public class ParallelAction : CoroutineAction
    {
        public ParallelAction(params IEnumerable[] enumerables)
        {
            if (enumerables == null)
                throw new ArgumentNullException(nameof(enumerables));
            Enumerables = enumerables;
        }

        public IEnumerable[] Enumerables { get; private set; }

        public override CoroutineActionBehavior Process(CoroutineThread thread, ref IEnumerable cor)
        {
            cor = thread.Executor.Parallel(Enumerables);
            Enumerables = null;
            return CoroutineActionBehavior.Push;
        }
    }
}