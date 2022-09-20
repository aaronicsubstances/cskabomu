using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    /// <summary>
    /// Provides helper methods for implementing <see cref="IRegistry"/> interface.
    /// </summary>
    public static class RegistryUtils
    {
        /// <summary>
        /// Provides an implementation of <see cref="IRegistry.Get"/> method which uses the
        /// <see cref="IRegistry.TryGet"/> method.
        /// </summary>
        /// <param name="instance">registry instance.</param>
        /// <param name="key">key to find</param>
        /// <returns>the second item of the pair returned by <see cref="IRegistry.TryGet"/> if
        /// the first item is true; else an exception is thrown.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="instance"/> argument is null</exception>
        /// <exception cref="NotInRegistryException">The first item of the pair returned by
        /// <see cref="IRegistry.TryGet"/> is false</exception>
        public static object Get(IRegistry instance, object key)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            var (present, value) = instance.TryGet(key);
            if (!present)
            {
                throw CreateNotInRegistryExceptionForKey(key);
            }
            return value;
        }

        /// <summary>
        /// Provides an implementation of <see cref="IRegistry.TryGetFirst"/> method which uses the
        /// <see cref="IRegistry.GetAll"/> method.
        /// </summary>
        /// <param name="instance">registry instance.</param>
        /// <param name="key">key to find</param>
        /// <param name="transformFunction">predicate/transform function</param>
        /// <returns>the first item in the result of <see cref="IRegistry.GetAll"/> method
        /// which satisfied the predicate condition determined by the transformFunction argument.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="instance"/> or
        /// <paramref name="transformFunction"/> argument is null</exception>
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

        /// <summary>
        /// Creates an instance of <see cref="NotInRegistryException"/> class with
        /// error message describing a missing registry key.
        /// </summary>
        /// <param name="key">the key which was not found in a registry</param>
        /// <returns>new instance of <see cref="NotInRegistryException"/> class</returns>
        public static NotInRegistryException CreateNotInRegistryExceptionForKey(object key)
        {
            return new NotInRegistryException($"No object found in registry for key: {key}");
        }
    }
}
