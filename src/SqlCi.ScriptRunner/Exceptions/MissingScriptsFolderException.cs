using System;
using System.Runtime.Serialization;

namespace SqlCi.ScriptRunner.Exceptions
{
    [Serializable]
    public class MissingScriptsFolderException : Exception
    {
        public MissingScriptsFolderException()
        {
        }

        public MissingScriptsFolderException(string message)
            : base(message)
        {
        }

        public MissingScriptsFolderException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected MissingScriptsFolderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}