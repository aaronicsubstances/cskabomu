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
            return new HierarchicalRegistry(parent, child);
        }

        public static T Get<T>(this IRegistry instance, string key)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            return (T)instance.Get(key);
        }

        public static T Get<T>(this IRegistry instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            return (T)instance.Get(typeof(T));
        }

        public static (bool, T) TryGet<T>(this IRegistry instance, string key)
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

        public static (bool, T) TryGet<T>(this IRegistry instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            var result = instance.TryGet(typeof(T));
            if (result.Item1)
            {
                return (true, (T)result.Item2);
            }
            else
            {
                return (false, default(T));
            }
        }

        public static IEnumerable<T> GetAll<T>(this IRegistry instance, string key)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            return instance.GetAll(key).Cast<T>();
        }

        public static IEnumerable<T> GetAll<T>(this IRegistry instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            return instance.GetAll(typeof(T)).Cast<T>();
        }
       
        public static (bool, U) TryGetFirst<T, U>(this IRegistry instance, string key, Func<T, (bool, U)> transformFunction)
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

        public static (bool, U) TryGetFirst<T, U>(this IRegistry instance, Func<T, (bool, U)> transformFunction)
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
            var result = instance.TryGetFirst(typeof(T), transformFunctionWrapper);
            if (result.Item1)
            {
                return (true, (U)result.Item2);
            }
            else
            {
                return (false, default(U));
            }
        }

        public static IMutableRegistry Remove<T>(this IMutableRegistry instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            return instance.Remove(typeof(T));
        }

        public static IMutableRegistry Add<T>(this IMutableRegistry instance, T value)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            return instance.Add(typeof(T), value);
        }

        public static IMutableRegistry AddLazy<T>(this IMutableRegistry instance, Func<T> valueGenerator)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (valueGenerator == null)
            {
                throw new ArgumentNullException(nameof(valueGenerator));
            }
            Func<object> lazyGenerator = RegistryUtils.MakeLazyGenerator(() => (object)valueGenerator.Invoke());
            return instance.AddGenerator(typeof(T), lazyGenerator);
        }

        public static IMutableRegistry AddGenerator<T>(this IMutableRegistry instance, Func<T> valueGenerator)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (valueGenerator == null)
            {
                throw new ArgumentNullException(nameof(valueGenerator));
            }
            Func<object> valueGeneratorWrapper = () => valueGenerator.Invoke();
            return instance.AddGenerator(typeof(T), valueGeneratorWrapper);
        }
    }
}
