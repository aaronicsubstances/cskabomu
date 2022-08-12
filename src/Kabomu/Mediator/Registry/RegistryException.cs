using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class RegistryException : Exception
    {
        public RegistryException()
        {
        }

        public RegistryException(string message) : base(message)
        {
        }

        public RegistryException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
