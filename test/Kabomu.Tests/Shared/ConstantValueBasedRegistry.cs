using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Tests.Shared
{
    public class ConstantValueBasedRegistry : IRegistry
    {
        private readonly object _value;

        public ConstantValueBasedRegistry(object value)
        {
            _value = value;
        }

        public object Get(object key)
        {
            return _value;
        }

        public IEnumerable<object> GetAll(object key)
        {
            return Enumerable.Repeat(_value, 1);
        }

        public (bool, object) TryGet(object key)
        {
            return (true, _value);
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            return transformFunction.Invoke(_value);
        }
    }
}
