using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class ConstantRegistryValueSource : IRegistryValueSource
    {
        public ConstantRegistryValueSource(object value)
        {
            Value = value;
        }

        public Type ValueType => Value?.GetType() ?? typeof(object);

        private object Value { get; }

        public object Get()
        {
            return Value;
        }
    }
}
