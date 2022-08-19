using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public static class RegistryExtensions
    {
        public static IRegistry Join(this IRegistry parent, IRegistry child)
        {
            if (child == null) return parent;
            return new HierarchicalRegistry(parent, child);
        }

        public static T Get<T>(this IRegistry instance, object key)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            return (T)instance.Get(key);
        }

        public static (bool, T) TryGet<T>(this IRegistry instance, object key)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            var result = instance.TryGet(key);
            if (result.Item1)
            {
                return (true, (T)result.Item2);
            }
            else
            {
                return (false, default(T));
            }
        }

        public static IEnumerable<T> GetAll<T>(this IRegistry instance, object key)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            return instance.GetAll(key).Cast<T>();
        }
       
        public static (bool, U) TryGetFirst<T, U>(this IRegistry instance, object key, Func<T, (bool, U)> transformFunction)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (transformFunction == null)
            {
                throw new ArgumentNullException(nameof(transformFunction));
            }
            Func<object, (bool, object)> transformFunctionWrapper = obj =>
            {
                var item = (T)obj;
                var transformedItem = transformFunction.Invoke(item);
                return transformedItem;
            };
            var result = instance.TryGetFirst(key, transformFunctionWrapper);
            if (result.Item1)
            {
                return (true, (U)result.Item2);
            }
            else
            {
                return (false, default(U));
            }
        }

        public static IMutableRegistry AddLazy(this IMutableRegistry instance, object key, Func<object> valueGenerator)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (valueGenerator == null)
            {
                throw new ArgumentNullException(nameof(valueGenerator));
            }
            Func<object> lazyGenerator = RegistryUtils.MakeLazyGenerator(() => valueGenerator.Invoke());
            return instance.AddGenerator(key, lazyGenerator);
        }
    }
}
