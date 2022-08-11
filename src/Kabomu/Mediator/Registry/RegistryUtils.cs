using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public static class RegistryUtils
    {
        public static object Get(IRegistry instance, object key)
        {
            var (present, value) = instance.TryGet(key);
            if (!present)
            {
                throw new NotInRegistryException($"No object found in registry for key: {key}");
            }
            return value;
        }

        public static (bool, object) TryGetFirst(IRegistry instance, object key, Func<object, (bool, object)> transformFunction)
        {
            return instance.GetAll(key)
                .Select(x => transformFunction.Invoke(x))
                .FirstOrDefault(x => x.Item1);
        }
    }
}
