using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class SingleEntryRegistry : IRegistry
    {
        private readonly Type _key;
        private readonly IRegistryValueSource _value;

        public SingleEntryRegistry(Type key, IRegistryValueSource valueSource)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _value = valueSource ?? throw new ArgumentNullException(nameof(valueSource));
        }

        public (bool, object) TryGet(object key)
        {
            if (key is Type typeKey && typeKey.IsAssignableFrom(_key))
            {
                return (true, _value.Get());
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
