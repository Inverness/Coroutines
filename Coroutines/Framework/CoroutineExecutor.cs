using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Coroutines.Framework
{
    /// <summary>
    /// Manages execution for threads of coroutines. Threads in this context are logical, software threads rather than
    /// hardware threads. This class is not thread-safe.
    /// </summary>
    /// <remarks>
    /// Coroutines are represented by non-generic IEnumerable objects. They should yield an IEnumerable to transfer
    /// execution to that coroutine, or yield a TimeSpan to delay execution for the specified amount of time.
    /// Any other yields will be ignored.
    /// </remarks>
    public class CoroutineExecutor
    {
        [ThreadStatic]
        private static Stack<CoroutineExecutor> t_currentExecutors;

        private readonly List<Stack<IEnumerator>> _stacks = new List<Stack<IEnumerator>>();
        private TimeSpan _time;
        private TimeSpan _elapsed;
        private Stack<IEnumerator> _executingStack;

        /// <summary>
        /// Gets the current time, accumulated from all ticks.
        /// </summary>
        public TimeSpan Time => _time;

        /// <summary>
        /// Gets whether a coroutine is being executed.
        /// </summary>
        public bool IsExecuting => _executingStack != null;

        /// <summary>
        /// Gets the amount of time that has elapsed since the previous tick. This is only valid while a coroutine is
        /// being executed.
        /// </summary>
        public TimeSpan ElapsedTime => _elapsed;

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
            if (_executingStack != null)
                throw new InvalidOperationException("Recursive ticking not allowed");

            _time += elapsed;

            int alive = 0;

            for (int i = 0; i < _stacks.Count; i++)
            {
                Stack<IEnumerator> stack = _stacks[i];

                if (stack.Count != 0 && TickStack(stack, elapsed))
                    alive++;
            }

            return alive;
        }

        /// <summary>
        /// Executes a coroutine in a new thread.
        /// </summary>
        /// <param name="enumerable">An enumerable object that can provide a coroutine.</param>
        public void Execute(IEnumerable enumerable)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            Stack<IEnumerator> freeStack = null;
            for (int i = 0; i < _stacks.Count; i++)
            {
                if (_stacks[i].Count == 0)
                    freeStack = _stacks[i];
            }

            if (freeStack == null)
            {
                freeStack = new Stack<IEnumerator>();
                _stacks.Add(freeStack);
            }

            freeStack.Push(enumerable.GetEnumerator());
        }

        /// <summary>
        /// Returns a coroutine that finishes once the specified amount of time has passed.
        /// </summary>
        /// <param name="seconds">The number of seconds to delay.</param>
        /// <returns>A coroutine producer.</returns>
        public IEnumerable Delay(double seconds)
        {
            return Delay(TimeSpan.FromSeconds(seconds));
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

            TimeSpan endTime = _time + duration;

            while (true)
            {
                if (_time < endTime)
                    yield return null;
                else
                    yield break;
            }
        }

        ///// <summary>
        ///// Ticks all coroutine threads until they have finished.
        ///// </summary>
        ///// <param name="timeFactor"></param>
        //public void FinishExecution(double? timeFactor = null)
        //{
        //    if (!timeFactor.HasValue)
        //        timeFactor = 1.0;
        //    if (timeFactor.Value <= 0)
        //        throw new ArgumentOutOfRangeException(nameof(timeFactor), "timeFactor must be greater than zero");

        //    var sw = Stopwatch.StartNew();
            
        //    TimeSpan previousTime = TimeSpan.Zero;
        //    int living;
        //    do
        //    {
        //        TimeSpan time = TimeSpan.FromTicks((long) (sw.ElapsedTicks * timeFactor.Value));

        //        TimeSpan elapsed = time - previousTime;

        //        previousTime = time;

        //        sw.Stop();
        //        living = Tick(elapsed);
        //        sw.Start();
        //    } while (living != 0);
        //}

        private bool TickStack(Stack<IEnumerator> stack, TimeSpan elapsed)
        {
            Stack<CoroutineExecutor> currentExecutors = t_currentExecutors;
            if (currentExecutors == null)
                t_currentExecutors = currentExecutors = new Stack<CoroutineExecutor>();

            bool result;
            IEnumerator top = stack.Peek();

            currentExecutors.Push(this);
            _executingStack = stack;
            _elapsed = elapsed;
            try
            {
                result = top.MoveNext();
            }
            finally
            {
                _executingStack = null;
                _elapsed = TimeSpan.Zero;
                currentExecutors.Pop();
            }

            Debug.Assert(stack.Count != 0 && stack.Peek() == top);

            if (result)
            {
                object current = top.Current;
                if (current != null)
                {
                    TimeSpan? nextDelay;
                    IEnumerable nextEnumerable;

                    if ((nextEnumerable = current as IEnumerable) != null)
                    {
                        stack.Push(nextEnumerable.GetEnumerator());
                    }
                    else if ((nextDelay = current as TimeSpan?) != null && nextDelay.Value.Ticks > 0)
                    {
                        stack.Push(Delay(nextDelay.Value).GetEnumerator());
                    }
                }
            }
            else
            {
                stack.Pop();
            }

            return stack.Count != 0;
        }
    }
}
