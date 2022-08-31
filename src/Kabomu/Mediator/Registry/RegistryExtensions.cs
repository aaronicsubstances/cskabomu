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
            if (parent == null) return child;
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
       
        public static (bool, T) TryGetFirst<T>(this IRegistry instance, object key, Func<object, (bool, T)> transformFunction)
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
                var transformedItem = transformFunction.Invoke(obj);
                return transformedItem;
            };
            var result = instance.TryGetFirst(key, transformFunctionWrapper);
            if (result.Item1)
            {
                return (true, (T)result.Item2);
            }
            else
            {
                return (false, default(T));
            }
        }

        public static IMutableRegistry AddLazy(this IMutableRegistry instance, object key, Func<object> valueGenerator)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            // let other argument be validated by object construction below
            Func<object> lazyValueGenerator = new LazyValueGeneratorInternal<object>(valueGenerator).Get;
            return instance.AddGenerator(key, lazyValueGenerator);
        }
    }
}
