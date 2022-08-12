using System;

namespace Kabomu.Mediator.ResponseRendering
{
    public class RenderException : Exception
    {
        public RenderException()
        {
        }

        public RenderException(string message) : base(message)
        {
        }

        public RenderException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}