using System.Collections;

namespace Coroutines.Framework
{
    public class ResultAction : CoroutineAction
    {
        public ResultAction(object result)
        {
            Value = result;
        }

        public object Value { get; private set; }

        public override CoroutineActionBehavior Process(CoroutineThread thread, ref IEnumerable cor)
        {
            thread.SetResult(Value);
            Value = null;
            return CoroutineActionBehavior.Pop;
        }
    }
}
