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
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            var (present, value) = instance.TryGet(key);
            if (!present)
            {
                throw new RegistryException($"No object found in registry for key: {key}");
            }
            return value;
        }

        public static (bool, object) TryGetFirst(IRegistry instance, object key, Func<object, (bool, object)> transformFunction)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (transformFunction == null)
            {
                throw new ArgumentNullException(nameof(transformFunction));
            }
            return instance.GetAll(key)
                .Select(x => transformFunction.Invoke(x))
                .FirstOrDefault(x => x.Item1);
        }

        public static Func<T> MakeLazyGenerator<T>(Func<T> valueGenerator)
        {
            Func<T> lazyValueGenerator = new LazyValueGenerator<T>(valueGenerator).Get;
            return lazyValueGenerator;
        }
    }
}
