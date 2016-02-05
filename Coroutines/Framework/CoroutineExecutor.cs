using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace Coroutines.Framework
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
        [ThreadStatic]
        private static Stack<CoroutineExecutor> t_currentExecutors;
        
        [DataMember(Name = "Threads")]
        private readonly List<CoroutineThread> _threads = new List<CoroutineThread>();

        [DataMember(Name = "Time")]
        private TimeSpan _time;
        private TimeSpan _elapsed;
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
        /// Gets the amount of time that has elapsed since the previous tick. This is only valid while a coroutine is
        /// being executed.
        /// </summary>
        public TimeSpan ElapsedTime => _elapsed;

        /// <summary>
        /// Gets a list of current coroutine threads.
        /// </summary>
        public IReadOnlyList<CoroutineThread> Threads => _threads; 

        /// <summary>
        /// Gets the thread's current executor if any. This will be the most nested executor if there is more than one.
        /// </summary>
        public static CoroutineExecutor Current
        {
            get
            {
                Stack<CoroutineExecutor> stack = t_currentExecutors;
                return stack != null && stack.Count != 0 ? stack.Peek() : null;
            }
        }

        /// <summary>
        /// Ticks all living coroutines, advancing time by the specified amount.
        /// </summary>
        /// <param name="elapsed">The time that has elapsed since the previous tick.</param>
        /// <returns>The number of living coroutine threads.</returns>
        public int Tick(TimeSpan elapsed)
        {
            if (elapsed.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            if (_executingThread != null)
                throw new InvalidOperationException("Recursive ticking not allowed");

            _time += elapsed;

            int alive = 0;

            for (int i = 0; i < _threads.Count; i++)
            {
                CoroutineThread thread = _threads[i];

                TickThread(thread, elapsed);

                if (thread.Status < CoroutineThreadStatus.Finished)
                    alive++;
            }

            return alive;
        }

        /// <summary>
        /// Executes a coroutine in a new thread.
        /// </summary>
        /// <param name="enumerable">An enumerable object that can provide a coroutine.</param>
        public CoroutineThread StartThread(IEnumerable<CoroutineAction> enumerable)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            var thread = new CoroutineThread(this, enumerable);
            _threads.Add(thread);
            return thread;
        }

        /// <summary>
        /// Returns a coroutine that finishes once the specified amount of time has passed.
        /// </summary>
        /// <param name="seconds">The number of seconds to delay.</param>
        /// <returns>A coroutine producer.</returns>
        public IEnumerable<CoroutineAction> DelaySeconds(double seconds)
        {
            return Delay(TimeSpan.FromSeconds(seconds));
        }

        /// <summary>
        /// Returns a coroutine that finishes once the specified amount of time has passed.
        /// </summary>
        /// <param name="duration">The amount of time to delay.</param>
        /// <returns>A coroutine producer.</returns>
        public IEnumerable<CoroutineAction> Delay(TimeSpan duration)
        {
            if (duration.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(duration));

            TimeSpan endTime = _time + duration;

            while (_time < endTime)
                yield return null;
        }

        /// <summary>
        /// Executes coroutines in parallel. Stops when any coroutine faults, or when all are finished.
        /// </summary>
        /// <param name="enumerables"></param>
        /// <returns></returns>
        public IEnumerable<CoroutineAction> Parallel(params IEnumerable<CoroutineAction>[] enumerables)
        {
            if (enumerables == null)
                throw new ArgumentNullException(nameof(enumerables));
            
            CoroutineThread[] threads = enumerables.Select(StartThread).ToArray();

            if (threads.Length == 0)
                yield break;

            while (true)
            {
                if (threads.Any(t => t.Status == CoroutineThreadStatus.Faulted))
                    yield break;

                if (threads.All(t => t.Status == CoroutineThreadStatus.Finished))
                    yield break;

                yield return null;
            }
        }

        /// <summary>
        /// Ticks all coroutine threads until they have finished. A Stopwatch will be used to measure elapsed time.
        /// </summary>
        public void Finish()
        {
            Finish(1.0);
        }

        /// <summary>
        /// Ticks all coroutine threads until they have finished. A Stopwatch will be used to measure elapsed time.
        /// The measured elapsed time will be multiplied by the specified factor.
        /// </summary>
        /// <param name="factor">A factor to multiply the measured elapsed time by.</param>
        public void Finish(double factor)
        {
            if (factor <= 0)
                throw new ArgumentOutOfRangeException(nameof(factor), "timeFactor must be greater than zero");

            var sw = Stopwatch.StartNew();

            TimeSpan previousTime = TimeSpan.Zero;
            int living;
            do
            {
                TimeSpan elapsed;
                checked
                {
                    TimeSpan newTime = TimeSpan.FromTicks((long) (sw.ElapsedTicks * factor));

                    elapsed = newTime - previousTime;

                    previousTime = newTime;
                }
                
                living = Tick(elapsed);
            } while (living != 0);
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

        private void TickThread(CoroutineThread thread, TimeSpan elapsed)
        {
            Stack<CoroutineExecutor> currentExecutors = t_currentExecutors;
            if (currentExecutors == null)
                t_currentExecutors = currentExecutors = new Stack<CoroutineExecutor>();

            currentExecutors.Push(this);
            _executingThread = thread;
            _elapsed = elapsed;
            try
            {
                thread.Tick();
            }
            finally
            {
                _executingThread = null;
                _elapsed = TimeSpan.Zero;
                currentExecutors.Pop();
            }
        }
    }
}
