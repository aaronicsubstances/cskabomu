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
            if (!typeof(T).IsAssignableFrom(value.GetType()))
            {
                throw new ArgumentException($"cannot add registry value of type '{typeof(T)} " +
                    $"from value source of type '{value.GetType()}'", nameof(value));
            }
            return instance.Add(typeof(T), value);
        }

        public static IMutableRegistry AddValueSource<T>(this IMutableRegistry instance, IRegistryValueSource valueSource)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (valueSource == null)
            {
                throw new ArgumentNullException(nameof(valueSource));
            }
            if (!typeof(T).IsAssignableFrom(valueSource.ValueType))
            {
                throw new ArgumentException($"cannot add registry value of type '{typeof(T)} " +
                    $"from value source of type '{valueSource.ValueType}'", nameof(valueSource));
            }
            return instance.AddValueSource(typeof(T), valueSource);
        }
    }
}
