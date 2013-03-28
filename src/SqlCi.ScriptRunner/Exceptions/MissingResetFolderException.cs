using System;
using System.Runtime.Serialization;

namespace SqlCi.ScriptRunner.Exceptions
{
    [Serializable]
    public class MissingResetFolderException : Exception
    {
        public MissingResetFolderException()
        {
        }

        public MissingResetFolderException(string message)
            : base(message)
        {
        }

        public MissingResetFolderException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected MissingResetFolderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}