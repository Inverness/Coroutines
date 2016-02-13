namespace Coroutines.Framework
{
    /// <summary>
    /// Describes the behavior of a CoroutineAction when it is processed.
    /// </summary>
    public enum CoroutineActionBehavior
    {
        /// <summary>
        /// The action will push a new frame onto the stack.
        /// </summary>
        Push,
        
        /// <summary>
        /// The action will pop the current frame from the stack.
        /// </summary>
        Pop,

        /// <summary>
        /// The action will yield execution.
        /// </summary>
        Yield
    }
}