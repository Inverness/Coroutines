using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Coroutines
{
    /// <summary>
    /// Describes a thread of execution for a coroutine.
    /// </summary>
    [DataContract(IsReference = true)]
    public sealed class CoroutineThread : IDisposable
    {
        [ThreadStatic]
        private static CoroutineThread t_currentThread;

        [DataMember(Name = "Stack")]
        private readonly Stack<IEnumerator> _stack;

        [DataMember(Name = "ElapsedTime")]
        private TimeSpan _elapsedTime;

        private object _result;
        private bool _hasResult;

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
        /// Gets or sets a user tag.
        /// </summary>
        public object Tag { get; set; }

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
        /// Gets the amount of time that has elapsed since the previous execution. This is only valid while a
        /// coroutine is being executed.
        /// </summary>
        public TimeSpan ElapsedTime => _elapsedTime;

        /// <summary>
        /// Gets the currently executing thread if any.
        /// </summary>
        public static CoroutineThread Current => t_currentThread;

        /// <summary>
        /// Gets the current thread result cast to the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetResult<T>()
        {
            if (!_hasResult)
                throw new InvalidOperationException("no result has been set");
            return (T) _result;
        }

        /// <summary>
        /// Gets the current thread result cast to the specified type, or default(T) if Result is null or not specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetResultOrDefault<T>()
        {
            if (_result != null)
                return (T) _result;
            return default(T);
        }

        /// <summary>
        /// Disposes the thread and sets the status to Finished.
        /// </summary>
        public void Dispose()
        {
            Dispose(null);
        }

        internal void SetResult(object result)
        {
            _result = result;
            _hasResult = true;
        }

        internal void ClearResult()
        {
            _result = null;
            _hasResult = false;
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
                {
                    IEnumerator top = _stack.Pop();
                    (top as IDisposable)?.Dispose();
                }
            }
            finally
            {
                Executor.OnThreadDisposed(this);
            }
        }

        internal void Tick(TimeSpan elapsed)
        {
            CoroutineThread oldCurrent = t_currentThread;
            t_currentThread = this;

            _elapsedTime = elapsed;
            
            try
            {
                bool endLoop = false;
                while (!endLoop)
                {
                    IEnumerable next = null;
                    FrameResult frameResult = RunFrame(_stack.Peek(), ref next);

                    switch (frameResult)
                    {
                        case FrameResult.Yield:
                            endLoop = true;
                            break;
                        case FrameResult.Pop:
                            IEnumerator oldTop = _stack.Pop();

                            (oldTop as IDisposable)?.Dispose();

                            if (_stack.Count == 0)
                            {
                                Dispose();
                                endLoop = true;
                            }
                            break;
                        case FrameResult.Push:
                            _stack.Push(next.GetEnumerator());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
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
                Debug.Assert(!_hasResult);
                t_currentThread = oldCurrent;
            }
        }

        private FrameResult RunFrame(IEnumerator frame, ref IEnumerable next)
        {
            Status = CoroutineThreadStatus.Executing;

            bool hasNext;
            try
            {
                hasNext = frame.MoveNext();
            }
            finally
            {
                Status = CoroutineThreadStatus.Yielded;

                // Clear the previous result if any. Results are only made available to the coroutine executing
                // immediately after a ResultAction is processed.
                ClearResult();
            }

            if (!hasNext)
                return FrameResult.Pop;

            // Yielding null yields the coroutine
            object result = frame.Current;

            switch (result)
            {
                case null:
                    return FrameResult.Yield;
                // An IEnumerable can be yielded directly to run it next, rather than requiring an action.
                case IEnumerable enumerable:
                    next = enumerable;
                    return FrameResult.Push;
                // An action will decide what to do next with the thread.
                case CoroutineAction action:
                {
                    IEnumerable actionNext = null;
                    CoroutineActionBehavior actionResult = action.Process(this, ref actionNext);

                    switch (actionResult)
                    {
                        case CoroutineActionBehavior.Yield:
                            return FrameResult.Yield;
                        case CoroutineActionBehavior.Push:
                            next = actionNext ?? throw new InvalidOperationException("no new frame specified");
                            return FrameResult.Push;
                        case CoroutineActionBehavior.Pop:
                            return FrameResult.Pop;
                        default:
                            throw new NotImplementedException();
                    }
                }
                default:
                    throw new InvalidOperationException("Unknown coroutine yielded type: " + result.GetType());
            }
        }

        private enum FrameResult
        {
            Yield,
            Pop,
            Push
        }
    }
}