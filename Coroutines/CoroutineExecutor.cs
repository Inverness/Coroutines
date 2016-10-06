using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Coroutines
{
    /// <summary>
    /// Manages execution for threads of coroutines. Threads in this context are logical, software threads rather than
    /// hardware threads. This class is not thread-safe.
    /// </summary>
    /// <remarks>
    /// Coroutines are represented by generic IEnumerable&lt;CoroutineAction&gt; objects. Yielding of null indicates
    /// that a coroutine should yield execution until the next tick.
    /// </remarks>
    [DataContract(IsReference = true)]
    public class CoroutineExecutor : IDisposable
    {
        
        [DataMember(Name = "Threads")]
        private readonly List<CoroutineThread> _threads = new List<CoroutineThread>();

        [DataMember(Name = "Time")]
        private TimeSpan _time;
        private CoroutineThread _executingThread;

        /// <summary>
        /// Gets the current time, accumulated from all ticks.
        /// </summary>
        public TimeSpan Time => _time;

        /// <summary>
        /// Gets whether a coroutine is being executed.
        /// </summary>
        public bool IsExecuting => _executingThread != null;

        /// <summary>
        /// Gets the currently executing thread if any.
        /// </summary>
        public CoroutineThread ExecutingThread => _executingThread;

        /// <summary>
        /// Gets a list of current coroutine threads.
        /// </summary>
        public IReadOnlyList<CoroutineThread> Threads => _threads;

        /// <summary>
        /// Ticks all living coroutines, advancing time by the specified amount.
        /// </summary>
        /// <param name="elapsed">The time that has elapsed since the previous tick.</param>
        /// <returns>Whether any threads are still alive.</returns>
        public bool Tick(TimeSpan elapsed)
        {
            if (elapsed.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            if (_executingThread != null)
                throw new InvalidOperationException("Recursive ticking not allowed");

            _time += elapsed;

            for (int i = 0; i < _threads.Count; i++)
            {
                CoroutineThread thread = _threads[i];

                _executingThread = thread;
                try
                {
                    thread.Tick(elapsed);
                }
                finally
                {
                    _executingThread = null;
                }
            }

            return _threads.Count != 0;
        }

        /// <summary>
        /// Executes a coroutine in a new thread.
        /// </summary>
        /// <param name="cor">An enumerable object that can provide a coroutine.</param>
        public CoroutineThread Start(IEnumerable cor)
        {
            if (cor == null)
                throw new ArgumentNullException(nameof(cor));

            var thread = new CoroutineThread(this, cor);
            _threads.Add(thread);
            return thread;
        }

        /// <summary>
        /// Returns a coroutine that finishes once the specified amount of time has passed.
        /// </summary>
        /// <param name="duration">The amount of time to delay.</param>
        /// <returns>A coroutine producer.</returns>
        public IEnumerable Delay(TimeSpan duration)
        {
            if (duration.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(duration));

            return DelayImpl(_time + duration);
        }

        /// <summary>
        /// Executes coroutines in parallel. Stops when any coroutine faults, or when all are finished.
        /// </summary>
        /// <param name="cors"></param>
        /// <returns></returns>
        public IEnumerable Parallel(params IEnumerable[] cors)
        {
            if (cors == null)
                throw new ArgumentNullException(nameof(cors));
            if (cors.Length < 2)
                throw new ArgumentException("must specify at least two coroutines", nameof(cors));
            
            return ParallelImpl(cors.Select(Start).ToArray());
        }

        public virtual void Dispose()
        {
            while (_threads.Count != 0)
                _threads[_threads.Count - 1].Dispose();
        }

        internal void OnThreadDisposed(CoroutineThread thread)
        {
            _threads.Remove(thread);
        }

        protected virtual void OnSerializing(StreamingContext context)
        {
            if (_executingThread != null)
                throw new InvalidOperationException("Can't serialize while executing");
        }

        protected virtual void OnDeserialized(StreamingContext context)
        {
            foreach (CoroutineThread thread in _threads)
                thread.Executor = this;
        }

        private IEnumerable DelayImpl(TimeSpan endTime)
        {
            while (_time < endTime)
                yield return null;
        }

        private static IEnumerable ParallelImpl(CoroutineThread[] threads)
        {
            while (true)
            {
                {
                    bool allFinished = true;

                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (int i = 0; i < threads.Length; i++)
                    {
                        if (threads[i].Status == CoroutineThreadStatus.Faulted)
                            yield break;
                        if (threads[i].Status != CoroutineThreadStatus.Finished)
                            allFinished = false;
                    }

                    if (allFinished)
                        yield break;
                }

                yield return null;
            }
        }

        [OnSerializing]
        private void OnSerializingCallback(StreamingContext context)
        {
            OnSerializing(context);
        }

        [OnDeserialized]
        private void OnDeserializedCallback(StreamingContext context)
        {
            OnDeserialized(context);
        }
    }
}
