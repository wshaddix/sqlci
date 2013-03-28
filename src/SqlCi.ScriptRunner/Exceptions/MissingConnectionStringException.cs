using System;
using System.Runtime.Serialization;

namespace SqlCi.ScriptRunner.Exceptions
{
    [Serializable]
    public class MissingConnectionStringException : Exception
    {
        public MissingConnectionStringException()
        {
        }

        public MissingConnectionStringException(string message)
            : base(message)
        {
        }

        public MissingConnectionStringException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected MissingConnectionStringException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}