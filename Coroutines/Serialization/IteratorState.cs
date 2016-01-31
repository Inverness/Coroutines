using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Coroutines.Serialization
{
    /// <summary>
    /// Represents the serializable state of an iterator.
    /// </summary>
    [DataContract]
    public sealed class IteratorState
    {
        public IteratorState()
        {
            Arguments = new Dictionary<string, object>();
            Variables = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets or sets the name of the type that declared the iterator.
        /// </summary>
        [DataMember]
        public string DeclaringTypeName { get; set; }

        /// <summary>
        /// Gets or sets the name of the iterator method.
        /// </summary>
        [DataMember]
        public string MethodName { get; set; }

        /// <summary>
        /// Gets or sets the state number.
        /// </summary>
        [DataMember]
        public int State { get; set; }

        /// <summary>
        /// Gets or sets the current iterator value.
        /// </summary>
        [DataMember]
        public object Current { get; set; }

        /// <summary>
        /// Gets or sets the instance.
        /// </summary>
        [DataMember]
        public object This { get; set; }

        /// <summary>
        /// Gets a dictionary containing argument values.
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Arguments { get; }

        /// <summary>
        /// Gets or sets a dictionary containing variable values.
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Variables { get; }
    }
}