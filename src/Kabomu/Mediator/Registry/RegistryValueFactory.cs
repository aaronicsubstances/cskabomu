using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class RegistryValueFactory<T> : IRegistryValueSource
    {
        public RegistryValueFactory(Func<T> valueGenerator)
        {
            ValueGenerator = valueGenerator ?? throw new ArgumentNullException(nameof(valueGenerator));
        }

        public Type ValueType => typeof(T);

        private Func<T> ValueGenerator { get; }

        public object Get()
        {
            var value = ValueGenerator.Invoke();
            return value;
        }
    }
}
