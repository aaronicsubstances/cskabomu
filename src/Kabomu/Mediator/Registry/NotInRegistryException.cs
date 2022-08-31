using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class NotInRegistryException : RegistryException
    {
        public NotInRegistryException(object key):
            base($"No object found in registry for key: {key}")
        {
        }
    }
}
