using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator
{
    public static class RegistryUtils
    {
        public static object Get(IRegistry instance, string key)
        {
            var (present, value) = instance.TryGet(key);
            if (!present)
            {
                throw new ArgumentException("key not found", nameof(key));
            }
            return value;
        }

        public static object Get(IRegistry instance, Type key)
        {
            var (present, value) = instance.TryGet(key);
            if (!present)
            {
                throw new ArgumentException("key not found", nameof(key));
            }
            return value;
        }

        public static object GetFirstNonNull(IRegistry instance, string key, Func<object, object> transformFunction)
        {
            return instance.GetAll(key)
                .Select(x => transformFunction.Invoke(x))
                .FirstOrDefault(x => x != null);
        }

        public static object GetFirstNonNull(IRegistry instance, Type key, Func<object, object> transformFunction)
        {
            return instance.GetAll(key)
                .Select(x => transformFunction.Invoke(x))
                .FirstOrDefault(x => x != null);
        }
    }
}
