using System;
using System.Collections;

namespace Coroutines
{
    /// <summary>
    /// An action that executes coroutines in parallel.
    /// </summary>
    public class ParallelAction : CoroutineAction
    {
        private IEnumerable[] _enumerables;

        public ParallelAction(params IEnumerable[] enumerables)
        {
            _enumerables = enumerables ?? throw new ArgumentNullException(nameof(enumerables));
        }
        
        // ReSharper disable once RedundantAssignment
        public override CoroutineActionBehavior Process(CoroutineThread thread, ref IEnumerable cor)
        {
            cor = thread.Executor.Parallel(_enumerables);
            _enumerables = null;
            return CoroutineActionBehavior.Push;
        }
    }
}