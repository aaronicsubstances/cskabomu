using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    /// <summary>
    /// Provides extension methods applicable to all implementations of <see cref="IRegistry"/> interface.
    /// </summary>
    public static class RegistryExtensions
    {
        /// <summary>
        /// Constructs a registry out of two existing registries, in which the second one is preferred,
        /// and the first is used as fallback when keys are not found in preferred registry.
        /// </summary>
        /// <param name="parent">the fallback registry. if null, preferred registry is returned.</param>
        /// <param name="child">the preferred registry. if null, child registry is returned.</param>
        /// <returns>an <see cref="IRegistry"/> implementation which "joins" the two registry arguments together; or
        /// null if both arguments are null.</returns>
        public static IRegistry Join(this IRegistry parent, IRegistry child)
        {
            if (parent == null) return child;
            if (child == null) return parent;
            return new HierarchicalRegistry(parent, child);
        }

        /// <summary>
        /// Provides typed version of <see cref="IRegistry.Get"/> method, for getting a
        /// value of a given type stored under a given key.
        /// </summary>
        /// <typeparam name="T">type of object to return</typeparam>
        /// <param name="instance">registry instance</param>
        /// <param name="key">key to find</param>
        /// <returns>one (or last) of the values at given key, cast to supplied type.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="instance"/> argument is null.</exception>
        public static T Get<T>(this IRegistry instance, object key)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            return (T)instance.Get(key);
        }

        /// <summary>
        /// Provides typed version of <see cref="IRegistry.TryGet"/> method, for getting a value
        /// of a given type stored under a given key.
        /// </summary>
        /// <typeparam name="T">type of object to return</typeparam>
        /// <param name="instance">registry instance</param>
        /// <param name="key">key to find</param>
        /// <returns>the pair returned by <see cref="IRegistry.TryGet"/>, in which the second item
        /// is cast to supplied type or changed to the default value of the supplied type, if the first item is
        /// true or false respectively</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="instance"/> argument is null.</exception>
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

        /// <summary>
        /// Provides typed version of <see cref="IRegistry.GetAll"/> method, for getting all values
        /// stored under a given key, and cast to a given type.
        /// </summary>
        /// <remarks>
        /// Due to use of iterator protocols, all casting operations may not have occured by the time a method
        /// call returns: only evaluation of return result guarantees that.
        /// </remarks>
        /// <typeparam name="T">type of object to return</typeparam>
        /// <param name="instance">registry instance</param>
        /// <param name="key">key to find</param>
        /// <returns>all values stored under given key, cast to given type</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="instance"/> argument is null.</exception>
        public static IEnumerable<T> GetAll<T>(this IRegistry instance, object key)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            return instance.GetAll(key).Cast<T>();
        }

        /// <summary>
        /// Provides typed version of <see cref="IRegistry.TryGetFirst"/> method,
        /// for getting the first value under a key which satisfies condition determined by a predicate/transform
        /// function.
        /// </summary>
        /// <typeparam name="T">type of object to return</typeparam>
        /// <param name="instance">registry instance</param>
        /// <param name="key">key to find</param>
        /// <param name="transformFunction">predicate/transform function</param>
        /// <returns>the pair returned by <see cref="IRegistry.TryGetFirst"/>,
        /// in which the second item is cast to supplied type or changed to the default value of the supplied type,
        /// if the first item is true or false respectively</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="instance"/> or <paramref name="transformFunction"/>
        /// arguments is null</exception>
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

        /// <summary>
        /// Provides implementations of <see cref="IMutableRegistry"/> with a method for adding
        /// a value generator which is invoked only once, and whose first time return result is
        /// reused for all subsequent attempted invocations.
        /// </summary>
        /// <param name="instance">registry instance</param>
        /// <param name="key">key to add with</param>
        /// <param name="valueGenerator">value generator procedure</param>
        /// <returns>the instance argument, to allow chaining of other methods.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="instance"/> or
        /// <paramref name="valueGenerator"/> argument is null</exception>
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
