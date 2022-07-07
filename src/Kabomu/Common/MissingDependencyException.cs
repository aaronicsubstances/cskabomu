using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Kabomu.Common
{
    public class MissingDependencyException : Exception
    {
        public MissingDependencyException()
        {
        }

        public MissingDependencyException(string message) : base(message)
        {
        }

        public MissingDependencyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MissingDependencyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
