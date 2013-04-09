using System;
using System.Runtime.Serialization;

namespace SqlCi.ScriptRunner.Exceptions
{
    [Serializable]
    public class MissingEnvironmentException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public MissingEnvironmentException()
        {
        }

        public MissingEnvironmentException(string message)
            : base(message)
        {
        }

        public MissingEnvironmentException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected MissingEnvironmentException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}