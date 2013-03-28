using System;
using System.Runtime.Serialization;

namespace SqlCi.ScriptRunner.Exceptions
{
    [Serializable]
    public class MissingReleaseNumberException : Exception
    {
        public MissingReleaseNumberException()
        {
        }

        public MissingReleaseNumberException(string message)
            : base(message)
        {
        }

        public MissingReleaseNumberException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected MissingReleaseNumberException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}