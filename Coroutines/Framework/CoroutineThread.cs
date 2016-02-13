using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Coroutines.Framework
{
    /// <summary>
    /// Describes a thread of execution for a coroutine.
    /// </summary>
    [DataContract(IsReference = true)]
    public sealed class CoroutineThread : IDisposable
    {
        [DataMember(Name = "Stack")]
        private readonly Stack<IEnumerator> _stack;

        [ThreadStatic]
        private static Stack<CoroutineThread> t_currentThreads;

        private TimeSpan _elapsed;

        internal CoroutineThread(CoroutineExecutor executor, IEnumerable enumerable)
        {
            Executor = executor;
            _stack = new Stack<IEnumerator>(4);
            _stack.Push(enumerable.GetEnumerator());
        }
        
        // Deserialization constructor
        private CoroutineThread()
        {
        }

        /// <summary>
        /// Gets the number of coroutine frames in the stack.
        /// </summary>
        public int FrameCount => _stack.Count;

        /// <summary>
        /// Gets the executor that created this thread.
        /// </summary>
        public CoroutineExecutor Executor { get; internal set; }

        /// <summary>
        /// Gets the status of this thread.
        /// </summary>
        [DataMember]
        public CoroutineThreadStatus Status { get; private set; }

        /// <summary>
        /// Gets the exception that caused this thread to fault if any.
        /// </summary>
        [DataMember]
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets the current coroutine result if any.
        /// </summary>
        public object Result { get; internal set; }

        /// <summary>
        /// Gets the amount of time that has elapsed since the previous execution. This is only valid while a
        /// coroutine is being executed.
        /// </summary>
        public TimeSpan ElapsedTime => _elapsed;

        /// <summary>
        /// Gets the currently executing thread if any.
        /// </summary>
        public static CoroutineThread Current
        {
            get
            {
                Stack<CoroutineThread> stack = t_currentThreads;
                return stack != null && stack.Count != 0 ? stack.Peek() : null;
            }
        }

        /// <summary>
        /// Gets the current thread result cast to the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetResult<T>()
        {
            return (T) Result;
        }

        /// <summary>
        /// Gets the current thread result cast to the specified type, or default(T) if Result is null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetResultOrDefault<T>()
        {
            if (Result != null)
                return (T) Result;
            return default(T);
        }

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
                    (_stack.Pop() as IDisposable)?.Dispose();
            }
            finally
            {
                Executor.OnThreadDisposed(this);
            }
        }

        internal void Tick(TimeSpan elapsed)
        {
            Stack<CoroutineThread> currentThreads = t_currentThreads;
            if (currentThreads == null)
                t_currentThreads = currentThreads = new Stack<CoroutineThread>();

            currentThreads.Push(this);

            _elapsed = elapsed;
            
            try
            {
                Stack<IEnumerator> stack = _stack;

                bool shouldYield = false;
                while (!shouldYield)
                {
                    IEnumerator top = stack.Peek();

                    Status = CoroutineThreadStatus.Executing;

                    bool hasResult = top.MoveNext();

                    Status = CoroutineThreadStatus.Yielded;

                    Debug.Assert(stack.Count != 0 && stack.Peek() == top);

                    bool shouldPop = false;
                    if (hasResult)
                    {
                        object result = top.Current;
                        if (result != null)
                        {
                            IEnumerable enumerable;
                            CoroutineAction action;

                            if ((enumerable = result as IEnumerable) != null)
                            {
                                stack.Push(enumerable.GetEnumerator());
                            }
                            else if ((action = result as CoroutineAction) != null)
                            {
                                IEnumerable next = null;
                                switch (action.Process(this, ref next))
                                {
                                    case CoroutineActionBehavior.Yield:
                                        shouldYield = true;
                                        break;
                                    case CoroutineActionBehavior.Push:
                                        if (next == null)
                                            throw new InvalidOperationException("no new frame specified");
                                        stack.Push(next.GetEnumerator());
                                        break;
                                    case CoroutineActionBehavior.Pop:
                                        shouldPop = true;
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }
                            else
                            {
                                shouldYield = true;
                            }
                        }
                        else
                        {
                            shouldYield = true;
                        }
                    }
                    else
                    {
                        shouldPop = true;
                    }

                    if (shouldPop)
                    {
                        stack.Pop();
                        (top as IDisposable)?.Dispose();

                        if (stack.Count == 0)
                        {
                            Dispose();
                            shouldYield = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispose(ex);
                throw;
            }
            finally
            {
                // Since actions are executed immediately, a coroutine lower on the stack will have no trouble receiving
                // the result of a coroutine it invoked. Otherwise, results are cleared after yielding.
                Result = null;

                currentThreads.Pop();
            }
        }
    }
}