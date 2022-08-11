using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator
{
    public static class RegistryExtensions
    {
        public static IRegistry Join(this IRegistry parent, IRegistry child)
        {
            throw new NotImplementedException();
        }

        public static T Get<T>(this IRegistry instance, string key)
        {
            return (T)instance.Get(key);
        }

        public static T Get<T>(this IRegistry instance)
        {
            return (T)instance.Get(typeof(T));
        }

        public static IEnumerable<T> GetAll<T>(this IRegistry instance, string key)
        {
            return instance.GetAll(key).Cast<T>();
        }

        public static IEnumerable<T> GetAll<T>(this IRegistry instance)
        {
            return instance.GetAll(typeof(T)).Cast<T>();
        }
       
        public static U GetFirstNonNull<T, U>(this IRegistry instance, string key, Func<T, U> transformFunction)
        {
            Func<object, object> transformFunctionWrapper = obj =>
            {
                var item = (T)obj;
                var transformedItem = transformFunction.Invoke(item);
                return transformFunction;
            };
            return (U)instance.GetFirstNonNull(key, transformFunctionWrapper);
        }

        public static U GetFirstNonNull<T, U>(this IRegistry instance, Func<T, U> transformFunction)
        {
            Func<object, object> transformFunctionWrapper = obj =>
            {
                var item = (T)obj;
                var transformedItem = transformFunction.Invoke(item);
                return transformFunction;
            };
            return (U)instance.GetFirstNonNull(typeof(T), transformFunctionWrapper);
        }

        public static void Add<T>(this IMutableRegistry instance, T value)
        {
            instance.Add(typeof(T), value);
        }

        public static void AddLazy<T>(this IMutableRegistry instance, Func<T> valueGenerator)
        {
            Func<object> valueGeneratorWrapper = () =>
            {
                var value = valueGenerator.Invoke();
                return value;
            };
            instance.AddLazy(typeof(T), valueGeneratorWrapper);
        }

        public static void Remove<T>(this IMutableRegistry instance)
        {
            instance.Remove(typeof(T));
        }
    }
}
