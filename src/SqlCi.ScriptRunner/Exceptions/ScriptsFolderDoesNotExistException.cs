using System;
using System.Runtime.Serialization;

namespace SqlCi.ScriptRunner.Exceptions
{
    [Serializable]
    public class ScriptsFolderDoesNotExistException : Exception
    {
        public ScriptsFolderDoesNotExistException()
        {
        }

        public ScriptsFolderDoesNotExistException(string message)
            : base(message)
        {
        }

        public ScriptsFolderDoesNotExistException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected ScriptsFolderDoesNotExistException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}