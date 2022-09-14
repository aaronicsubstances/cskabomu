using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    /// <summary>
    /// Provides an implementation of <see cref="IRegistry"/> which is equivalent to
    /// a registry which always has no values.
    /// </summary>
    public class EmptyRegistry : IRegistry
    {
        /// <summary>
        /// Provides singleton empty registry.
        /// </summary>
        public static readonly IRegistry Instance = new EmptyRegistry();

        private readonly IEnumerable<object> _getAllRetVal = new object[0];

        private EmptyRegistry()
        {
        }

        /// <summary>
        /// Always returns (false, null) to indicate emptiness.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>(false, null)</returns>
        public (bool, object) TryGet(object key)
        {
            return (false, null);
        }

        /// <summary>
        /// Always throws an instance of <see cref="NotInRegistryException"/> class to 
        /// indicate emptiness.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="NotInRegistryException"></exception>
        public object Get(object key)
        {
            throw new NotInRegistryException(key);
        }

        /// <summary>
        /// Always returns (false, null) to indicate emptiness.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="transformFunction"></param>
        /// <returns>(false, null)</returns>
        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            return (false, null);
        }

        /// <summary>
        /// Always returns an empty list to indicate emptiness.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>empty list</returns>
        public IEnumerable<object> GetAll(object key)
        {
            return _getAllRetVal;
        }
    }
}
