using System;
using System.Runtime.Serialization;

namespace Kabomu.Mediator.ResponseRendering
{
    public class NoSuchRendererException : Exception
    {
        public NoSuchRendererException()
        {
        }

        public NoSuchRendererException(string message) : base(message)
        {
        }

        public NoSuchRendererException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}