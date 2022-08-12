using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator
{
    public abstract class MediatorQuasiWebException : Exception
    {
        protected MediatorQuasiWebException()
        {
        }

        protected MediatorQuasiWebException(string message) : base(message)
        {
        }

        protected MediatorQuasiWebException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
