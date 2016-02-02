using System;
using System.Collections.Generic;

namespace Coroutines.Framework
{
    /// <summary>
    /// An action that executes coroutines in parallel.
    /// </summary>
    public sealed class ParallelAction : CoroutineAction
    {
        public ParallelAction(params IEnumerable<CoroutineAction>[] enumerables)
        {
            if (enumerables == null)
                throw new ArgumentNullException(nameof(enumerables));
            Enumerables = enumerables;
        }

        public IEnumerable<CoroutineAction>[] Enumerables { get; }

        public override IEnumerable<CoroutineAction> GetNext(CoroutineThread thread)
        {
            return thread.Executor.Parallel(Enumerables);
        }
    }
}