using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class TypeBasedRegistryKey
    {
        public TypeBasedRegistryKey(Type type)
        {
            TypeValue = type ?? throw new ArgumentNullException(nameof(type));
        }

        public Type TypeValue { get; }
    }
}
