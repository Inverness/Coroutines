namespace Coroutines.Framework
{
    /// <summary>
    /// Describes the status of a coroutine thread.
    /// </summary>
    public enum CoroutineThreadStatus
    {
        /// <summary>
        /// The thread has not yet executed or has yielded after execution.
        /// </summary>
        Yielded,

        /// <summary>
        /// The thread is currently executing a coroutine.
        /// </summary>
        Executing,

        /// <summary>
        /// The thread has finished execution without an error.
        /// </summary>
        Finished,

        /// <summary>
        /// The thread has finished execution due to an error.
        /// </summary>
        Faulted
    }
}