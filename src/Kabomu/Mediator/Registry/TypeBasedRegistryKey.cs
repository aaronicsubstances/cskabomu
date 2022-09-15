using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    /// <summary>
    /// Wraps <see cref="Type"/> objects for use as keys in instances of
    /// <see cref="IRegistry"/> interface.
    /// </summary>
    /// <remarks>
    /// The purpose of this class is to not use type objects directly as registry keys
    /// throughout the Kabomu library, and leave the decision to use type objects directly
    /// as registry keys to clients.
    /// </remarks>
    public class TypeBasedRegistryKey
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="type">the type object to wrap</param>
        /// <exception cref="ArgumentNullException">The <paramref name="type"/> argument is null</exception>
        public TypeBasedRegistryKey(Type type)
        {
            TypeValue = type ?? throw new ArgumentNullException(nameof(type));
        }

        /// <summary>
        /// Gets the type object supplied at construction time.
        /// </summary>
        public Type TypeValue { get; }
    }
}
