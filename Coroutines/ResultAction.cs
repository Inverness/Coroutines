using System.Collections;

namespace Coroutines
{
    public class ResultAction : CoroutineAction
    {
        private object _value;

        public ResultAction(object result)
        {
            _value = result;
        }

        public override CoroutineActionBehavior Process(CoroutineThread thread, ref IEnumerable cor)
        {
            thread.SetResult(_value);
            _value = null;
            return CoroutineActionBehavior.Pop;
        }
    }
}
