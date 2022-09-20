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
    /// The purpose of this class is so as not use type objects directly as registry keys
    /// throughout the Kabomu.Mediator quasi web framework, and instead leave that decision to clients.
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

        /// <summary>
        /// Returns a string representing the name of the <see cref="TypeValue"/> property.
        /// </summary>
        public override string ToString()
        {
            return TypeValue.ToString();
        }
    }
}
