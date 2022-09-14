using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    /// <summary>
    /// Represents conceptually a dictionary of keys and values of any type, in which keys can have multiple values.
    /// Important extensions are <see cref="IMutableRegistry"/>, <see cref="Handling.IContext"/> and
    /// <see cref="Handling.IContextRequest"/> interfaces.
    /// </summary>
    /// <remarks>
    /// The purpose of this interface and its implementations, is to have better patterns for using request attributes
    /// to pass data between middleware and handler functions. Such request attributes 
    /// schemes are usually implemented with single mutable dictionary per request, so that all request handlers read and write to 
    /// the same dictionary for each request, and must be careful to avoid unintended use of same key for different values.
    /// <para>
    /// With this interface and related classses, Kabomu.Mediator framework gives quasi http handlers the 
    /// ability to limit the visibility of request attributes, so that only the intended target http handlers
    /// see the attributes. This reduces the chances of key clashes.
    /// </para>
    /// </remarks>
    public interface IRegistry
    {
        /// <summary>
        /// Gets one of the objects which exist under a given key. For <see cref="IMutableRegistry"/> implementations,
        /// it is the last added object which should be returned.
        /// </summary>
        /// <param name="key">the key to search with</param>
        /// <returns>if objects exist for the given key, one of those objects is
        /// returned as the second item in a pair in which the first item will be true.
        /// Else (false, null) pair will be returned.</returns>
        (bool, object) TryGet(object key);

        /// <summary>
        /// Gets all objects which exist under a given key. For <see cref="IMutableRegistry"/> implementations,
        /// the objects should be returned in LIFO order, ie in reverse order of addition.
        /// </summary>
        /// <remarks>
        /// Implementations should try and use iterator protocols to generate the object list wherever possible.
        /// </remarks>
        /// <param name="key">the key to search with</param>
        /// <returns>all objects which exist for the given key, or an empty list.</returns>
        IEnumerable<object> GetAll(object key);

        /// <summary>
        /// Gets one of the objects which exist under a given key, and fails if no objects exist under the key.
        /// For <see cref="IMutableRegistry"/> implementations, it is the last added object which should be returned.
        /// </summary>
        /// <param name="key">the key to search with</param>
        /// <returns>one of the objects which exist under given key</returns>
        /// <exception cref="NotInRegistryException">If no objects exist under given key.</exception>
        object Get(object key);

        /// <summary>
        /// Provides an efficient way to search across all objects under a key without having to
        /// fetch all of those objects first.
        /// </summary>
        /// <remarks>
        /// The search makes uses of a predicate/transformation function, and returns the first
        /// pair it receives from that function when the received pair has its first item equal to true.
        /// </remarks>
        /// <param name="key">the key whose values are related to the desired object</param>
        /// <param name="transformFunction">a function which determines the desired object to return,
        /// and can return a transformation of any of the values under the key.</param>
        /// <returns>first return pair from transformFunction argument with a value of true for its first item, or
        /// (false, null) pair if there are no values under the given key, or if transformFunction argument
        /// does not return any pair with a value of true in its first item.</returns>
        (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction);
    }
}
