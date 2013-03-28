using System;
using System.Runtime.Serialization;

namespace SqlCi.ScriptRunner.Exceptions
{
    [Serializable]
    public class MissingScriptTableException : Exception
    {
        public MissingScriptTableException()
        {
        }

        public MissingScriptTableException(string message)
            : base(message)
        {
        }

        public MissingScriptTableException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected MissingScriptTableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}