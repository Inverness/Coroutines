using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Coroutines.Framework
{
    /// <summary>
    /// Describes a thread of execution for a coroutine.
    /// </summary>
    public sealed class CoroutineThread : IDisposable
    {
        private readonly CoroutineExecutor _executor;
        private readonly Stack<IEnumerator<CoroutineAction>> _stack;

        internal CoroutineThread(CoroutineExecutor executor, IEnumerable<CoroutineAction> enumerable)
        {
            _executor = executor;
            _stack = new Stack<IEnumerator<CoroutineAction>>(4);
            _stack.Push(enumerable.GetEnumerator());
        }

        /// <summary>
        /// Gets the number of coroutine frames in the stack.
        /// </summary>
        public int FrameCount => _stack.Count;

        /// <summary>
        /// Gets the executor that created this thread.
        /// </summary>
        public CoroutineExecutor Executor => _executor;

        /// <summary>
        /// Gets the status of this thread.
        /// </summary>
        public CoroutineThreadStatus Status { get; private set; }

        /// <summary>
        /// Gets the exception that caused this thread to fault if any.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Disposes the thread and sets the status to Finished.
        /// </summary>
        public void Dispose()
        {
            Dispose(null);
        }

        internal void Dispose(Exception ex)
        {
            if (Status >= CoroutineThreadStatus.Finished)
                return;

            Exception = ex;
            Status = ex != null ? CoroutineThreadStatus.Faulted : CoroutineThreadStatus.Finished;
            try
            {
                while (_stack.Count != 0)
                    _stack.Pop().Dispose();
            }
            finally
            {
                _executor.OnThreadDisposed(this);
            }
        }

        internal void Tick()
        {
            Stack<IEnumerator<CoroutineAction>> stack = _stack;

            // Loop continues until null
            while (true)
            {
                IEnumerator<CoroutineAction> top = stack.Peek();

                bool result;
                try
                {
                    Status = CoroutineThreadStatus.Executing;

                    result = top.MoveNext();

                    Status = CoroutineThreadStatus.Yielded;
                }
                catch (Exception ex)
                {
                    Dispose(ex);
                    throw;
                }

                Debug.Assert(stack.Count != 0 && stack.Peek() == top);

                if (result)
                {
                    CoroutineAction action = top.Current;
                    IEnumerable<CoroutineAction> next;

                    if ((next = action?.GetNext(this)) != null)
                    {
                        stack.Push(next.GetEnumerator());
                        // Actions are processed immediately without yielding.
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    stack.Pop();
                    top.Dispose();

                    if (stack.Count == 0)
                    {
                        Dispose();
                        break;
                    }
                }
            }
        }
    }
}