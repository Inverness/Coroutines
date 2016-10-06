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
            if (enumerables == null)
                throw new ArgumentNullException(nameof(enumerables));
            _enumerables = enumerables;
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