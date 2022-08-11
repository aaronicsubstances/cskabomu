using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class NotInRegistryException : Exception
    {
        public NotInRegistryException(string message) : base(message)
        {
        }
    }
}
