using System;
using System.Runtime.Serialization;

namespace SqlCi.ScriptRunner.Exceptions
{
    [Serializable]
    public class ResetFolderDoesNotExistException : Exception
    {
        public ResetFolderDoesNotExistException()
        {
        }

        public ResetFolderDoesNotExistException(string message)
            : base(message)
        {
        }

        public ResetFolderDoesNotExistException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected ResetFolderDoesNotExistException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}