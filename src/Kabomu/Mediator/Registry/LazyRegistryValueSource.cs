using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class LazyRegistryValueSource<T> : IRegistryValueSource
    {
        private readonly object _lock = new object();
        private bool _valueSet;
        private T _value;

        public LazyRegistryValueSource(Func<T> valueGenerator)
        {
            ValueGenerator = valueGenerator ?? throw new ArgumentNullException(nameof(valueGenerator));
        }

        public Type ValueType => typeof(T);

        private Func<T> ValueGenerator { get; }

        public T Get()
        {
            // use recommended double condition check strategy to implement lazy loading pattern
            if (!_valueSet)
            {
                lock (_lock)
                {
                    if (!_valueSet)
                    {
                        _value = ValueGenerator.Invoke();
                        _valueSet = true;
                    }
                }
            }
            return _value;
        }

        object IRegistryValueSource.Get()
        {
            throw new NotImplementedException();
        }
    }
}
