using System;
using System.Runtime.Serialization;

namespace SqlCi.ScriptRunner.Exceptions
{
    [Serializable]
    public class NotVerifiedException : Exception
    {
        public NotVerifiedException()
        {
        }

        public NotVerifiedException(string message)
            : base(message)
        {
        }

        public NotVerifiedException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected NotVerifiedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}