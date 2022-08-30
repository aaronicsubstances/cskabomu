using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    internal class LazyValueGeneratorInternal<T>
    {
        private readonly object _lock = new object();
        private bool _valueSet;
        private T _value;

        public LazyValueGeneratorInternal(Func<T> valueGenerator)
        {
            ValueGenerator = valueGenerator ?? throw new ArgumentNullException(nameof(valueGenerator));
        }

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
    }
}
