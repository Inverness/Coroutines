using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            ce.StartThread(FrameworkCoroutines.Simple(3));

            ce.Finish();
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
