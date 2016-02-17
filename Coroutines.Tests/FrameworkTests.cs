using System;
using System.Collections.Generic;
using System.Diagnostics;
using Coroutines.Framework;
using Xunit;
using Xunit.Abstractions;

namespace Coroutines.Tests
{
    public class FrameworkTests
    {
        private readonly ITestOutputHelper _out;

        public FrameworkTests(ITestOutputHelper output)
        {
            _out = output;
        }

        [Fact]
        public void SimpleExecute()
        {
            var ce = new CoroutineExecutor();

            ce.Start(FrameworkCoroutines.Simple(3));

            Run(ce);
        }

        private static void Run(CoroutineExecutor ce)
        {
            Stopwatch sw = Stopwatch.StartNew();

            TimeSpan previousTime = TimeSpan.Zero;

            TimeSpan elapsed;
            do
            {
                checked
                {
                    TimeSpan newTime = sw.Elapsed;

                    elapsed = newTime - previousTime;

                    previousTime = newTime;
                }
            } while (ce.Tick(elapsed));
        }
    }

    public class FrameworkCoroutines
    {
        public static IEnumerable<CoroutineAction> Simple(int r)
        {
            DoWork(r);

            r *= 2;

            yield return null;

            DoWork(r);

            r *= 3;

            yield return null;

            DoWork(r);
        }

        private static void DoWork(int a)
        {
            
        }
    }
}
