using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    /// <summary>
    /// Default implementation of <see cref="IMutableRegistry"/> interface.
    /// </summary>
    public class DefaultMutableRegistry : IMutableRegistry
    {
        private readonly IDictionary<object, LinkedList<Func<object>>> _entries;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public DefaultMutableRegistry()
        {
            _entries = new Dictionary<object, LinkedList<Func<object>>>();
        }

        /// <summary>
        /// Adds a new key value pair. Multiple values are allowed for a key, and stored 
        /// in LIFO order for retrievals.
        /// </summary>
        /// <param name="key">key to use for storage</param>
        /// <param name="value">value to store under given key</param>
        /// <returns>the instance on which this method was called, for chaining more mutable operations.</returns>
        public IMutableRegistry Add(object key, object value)
        {
            return AddGenerator(key, () => value);
        }

        /// <summary>
        /// Adds a procedure which can generate values for a given key. Multiple procedures
        /// are allowed for a key, and stored in LIFO order for retrievals.
        /// </summary>
        /// <remarks>
        /// This class does not impose any requirements on how a procedure generates its values with this method.
        /// </remarks>
        /// <param name="key">key to use for storage</param>
        /// <param name="valueGenerator">procedure to store under given key</param>
        /// <returns>the instance on which this method was called, for chaining more mutable operations.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="valueGenerator"/> argument is null.</exception>
        public IMutableRegistry AddGenerator(object key, Func<object> valueGenerator)
        {
            if (valueGenerator == null)
            {
                throw new ArgumentNullException(nameof(valueGenerator));
            }
            LinkedList<Func<object>> selectedEntries;
            if (_entries.ContainsKey(key))
            {
                selectedEntries = _entries[key];
            }
            else
            {
                selectedEntries = new LinkedList<Func<object>>();
                _entries.Add(key, selectedEntries);
            }
            // insert in LIFO order.
            selectedEntries.AddFirst(valueGenerator);
            return this;
        }

        /// <summary>
        /// Removes a key and all values stored under the key if it exists.
        /// </summary>
        /// <param name="key">key to remove with its values. ignored if key is not found.</param>
        /// <returns>the instance on which this method was called, for chaining more mutable operations.</returns>
        public IMutableRegistry Remove(object key)
        {
            _entries.Remove(key);
            return this;
        }

        /// <summary>
        /// Gets the last object added for a given key.
        /// </summary>
        /// <param name="key">the key to search with</param>
        /// <returns>if objects exist for the given key, the one which was added last is
        /// returned as the second item in a pair in which the first item will be true;
        /// else (false, null) pair will be returned.</returns>
        public (bool, object) TryGet(object key)
        {
            if (_entries.ContainsKey(key))
            {
                var valueGenerator = _entries[key].First.Value;
                var value = valueGenerator.Invoke();
                return (true, value);
            }
            return (false, null);
        }

        /// <summary>
        /// Gets the last object added for a given key, and fails key does not exist.
        /// </summary>
        /// <param name="key">the key to search with</param>
        /// <returns>the last object added for key argument.</returns>
        /// <exception cref="NotInRegistryException">The <paramref name="key"/> argument was not found.</exception>
        public object Get(object key)
        {
            if (_entries.ContainsKey(key))
            {
                var valueGenerator = _entries[key].First.Value;
                var value = valueGenerator.Invoke();
                return value;
            }
            throw new NotInRegistryException(key);
        }

        /// <summary>
        /// Gets all objects stored under a given key in reverse order of their addition order,
        /// so that the last added object is the first in the returned list, and the first added
        /// object is the last in the returned list.
        /// </summary>
        /// <param name="key">the key to search with</param>
        /// <returns>if key is found, all its values are returned; else an empty list is returned.</returns>
        public IEnumerable<object> GetAll(object key)
        {
            if (_entries.ContainsKey(key))
            {
                foreach (var valueGenerator in _entries[key])
                {
                    var value = valueGenerator.Invoke();
                    yield return value;
                }
            }
        }

        /// <summary>
        /// Searches across all objects under a key for the first of them which satisfies some unknown condition, and
        /// if one is found returns the object or a transformation of it.
        /// </summary>
        /// <remarks>
        /// The search makes uses of a predicate/transformation function. If a call to the function returns a pair
        /// with a first item value of true, then the argument with which the function was called is taken to satisfy
        /// some unknown condition.
        /// </remarks>
        /// <param name="key">the key whose values are related to the desired object</param>
        /// <param name="transformFunction">a function which determines the desired object to return,
        /// and can return a transformation of any of the values under the key.</param>
        /// <returns>first return pair from transformFunction argument with a value of true for its first item, or
        /// (false, null) pair if there are no values under the given key, or if transformFunction argument
        /// does not return any pair with a value of true in its first item.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="transformFunction"/> argument is null.</exception>
        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            if (transformFunction == null)
            {
                throw new ArgumentNullException(nameof(transformFunction));
            }
            if (_entries.ContainsKey(key))
            {
                foreach (var valueGenerator in _entries[key])
                {
                    var value = valueGenerator.Invoke();
                    var result = transformFunction.Invoke(value);
                    if (result.Item1)
                    {
                        return result;
                    }
                }
            }
            return (false, null);
        }
    }
}
