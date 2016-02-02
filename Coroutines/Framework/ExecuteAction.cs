using System;
using System.Collections.Generic;

namespace Coroutines.Framework
{
    /// <summary>
    /// An action that executes a coroutine on the current thread.
    /// </summary>
    public sealed class ExecuteAction : CoroutineAction
    {
        public ExecuteAction(IEnumerable<CoroutineAction> enumerable)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            Enumerable = enumerable;
        }

        public IEnumerable<CoroutineAction> Enumerable { get; }

        public override IEnumerable<CoroutineAction> GetNext(CoroutineThread thread)
        {
            return Enumerable;
        }
    }
}