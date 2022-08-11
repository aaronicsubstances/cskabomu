using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class MultiValueRegistry : IRegistry
    {
        private readonly ICollection<object> _values;

        public MultiValueRegistry(ICollection<object> values)
        {
            _values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public (bool, object) TryGet(object key)
        {
            if (key is Type typeKey)
            {
                foreach (var value in _values)
                {
                    if (value != null && typeKey.IsAssignableFrom(value.GetType()))
                    {
                        return (true, value);
                    }
                }
            }
            return (false, null);
        }

        public IEnumerable<object> GetAll(object key)
        {
            var selected = new List<object>();
            if (key is Type typeKey)
            {
                foreach (var value in _values)
                {
                    if (value != null && typeKey.IsAssignableFrom(value.GetType()))
                    {
                        selected.Add(value);
                    }
                }
            }
            return selected;
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            if (key is Type typeKey)
            {
                foreach (var value in _values)
                {
                    if (value != null && typeKey.IsAssignableFrom(value.GetType()))
                    {
                        var result = transformFunction.Invoke(value);
                        if (result.Item1)
                        {
                            return result;
                        }
                    }
                }
            }
            return (false, null);
        }

        public object Get(object key)
        {
            return RegistryUtils.Get(this, key);
        }
    }
}
