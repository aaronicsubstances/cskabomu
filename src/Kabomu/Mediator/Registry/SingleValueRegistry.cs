using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class SingleValueRegistry : IRegistry
    {
        private readonly object _value;

        public SingleValueRegistry(object value)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public (bool, object) TryGet(object key)
        {
            if (key is Type typeKey && typeKey.IsAssignableFrom(_value.GetType()))
            {
                return (true, _value);
            }
            return (false, null);
        }

        public IEnumerable<object> GetAll(object key)
        {
            var (present, value) = TryGet(key);
            if (present)
            {
                return Enumerable.Repeat(value, 1);
            }
            return Enumerable.Empty<object>();
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            var (present, value) = TryGet(key);
            if (present)
            {
                return transformFunction.Invoke(value);
            }
            return (false, null);
        }

        public object Get(object key)
        {
            return RegistryUtils.Get(this, key);
        }
    }
}
