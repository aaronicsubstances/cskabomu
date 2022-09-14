using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    /// <summary>
    /// Extends <see cref="IRegistry"/> interface to support mutable operations.
    /// An important extension is the <see cref="Handling.IContextRequest"/> interface.
    /// </summary>
    public interface IMutableRegistry : IRegistry
    {
        /// <summary>
        /// Adds a new key value pair. Multiple values should be allowed for a key, and stored 
        /// in LIFO order for retrievals.
        /// </summary>
        /// <param name="key">key to use for storage</param>
        /// <param name="value">value to store under given key</param>
        /// <returns>the instance on which this method was called, for chaining more mutable operations.</returns>
        IMutableRegistry Add(object key, object value);

        /// <summary>
        /// Adds to registry a procedure which can generate values for a given key. Multiple procedures
        /// should be allowed for a key, and stored in LIFO order for retrievals.
        /// </summary>
        /// <remarks>
        /// In general this method does not impose any requirements on how a procedure generates its values. Hence
        /// a procedure when called can just return the same value every time, or create a new value every time,
        /// return previously generated values, etc. It is completely up to a procedure's implementation.
        /// </remarks>
        /// <param name="key">key to use for storage</param>
        /// <param name="valueGenerator">procedure to store under given key</param>
        /// <returns>the instance on which this method was called, for chaining more mutable operations.</returns>
        IMutableRegistry AddGenerator(object key, Func<object> valueGenerator);

        /// <summary>
        /// Removes all values stored under a key. No error should occur if a non-existent key is specified.
        /// </summary>
        /// <param name="key">key to remove with its values.</param>
        /// <returns>the instance on which this method was called, for chaining more mutable operations.</returns>
        IMutableRegistry Remove(object key);
    }
}
