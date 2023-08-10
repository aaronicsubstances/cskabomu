using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    /// <summary>
    /// Implementation of <see cref="IRegistry"/> which is based on two existing registries, one designated "parent", 
    /// and the other designated "child". Keys are first looked up in the child registry, and it is only when 
    /// a key is not found in a child, will the parent registry be considered.
    /// </summary>
    /// <remarks>
    /// The <see cref="GetAll(object)"/> concatenates the results from both registries, with those from the
    /// child in front. If the parent registry returns its results for lazy evaluation, those results will only be
    /// evaluated after the child's results are exhausted, and may not be evaluated at all if the child's results
    /// are enough.
    /// </remarks>
    public class HierarchicalRegistry : IRegistry
    {
        private readonly IRegistry _parent;
        private readonly IRegistry _child;

        /// <summary>
        /// Creates a new instance out of two existing registries, with the 
        /// second being preferred for look up over the first.
        /// </summary>
        /// <param name="parent">the fallback registry.</param>
        /// <param name="child">the preferred registry</param>
        /// <exception cref="ArgumentNullException">The <paramref name="child"/> or <paramref name="parent"/> argument
        /// is null.</exception>
        public HierarchicalRegistry(IRegistry parent, IRegistry child)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _child = child ?? throw new ArgumentNullException(nameof(child));
        }

        /// <summary>
        /// Uses the preferred and fallback registries supplied at construction time to get
        /// a value for a given key.
        /// </summary>
        /// <param name="key">key to find.</param>
        /// <returns>a pair with true as its first value if and only if key is found in either preferred or
        /// fallback registries</returns>
        public (bool, object) TryGet(object key)
        {
            var result = _child.TryGet(key);
            if (result.Item1)
            {
                return result;
            }
            return _parent.TryGet(key);
        }

        /// <summary>
        /// Looks up preferred and fallback registries supplied at construction time to get
        /// the first value which satisfies a given predicate/transform function for a key.
        /// </summary>
        /// <param name="key">key to find.</param>
        /// <param name="transformFunction">predicate/transform function to apply first to
        /// preferred registry, and then to fallback registry. The first pair returned from the function with its first
        /// item equal to true is returned from this method.</param>
        /// <returns>pair returned from preferred registry or fallback registry,
        /// whose first item is true if and only if key argument is found in either preferred or fallback registry,
        /// and also happens to meet predicate condition determined by transformFunction argument.</returns>
        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            var result = _child.TryGetFirst(key, transformFunction);
            if (result.Item1)
            {
                return result;
            }
            return _parent.TryGetFirst(key, transformFunction);
        }

        /// <summary>
        /// Uses the preferred and fallback registries supplied at construction time to get
        /// a value for a given key, and should fail if key is not found in either registries.
        /// </summary>
        /// <param name="key">key to find.</param>
        /// <returns>value present for key in preferred or fallback registries</returns>
        public object Get(object key)
        {
            var result = _child.TryGet(key);
            if (result.Item1)
            {
                return result.Item2;
            }
            return _parent.Get(key);
        }

        /// <summary>
        /// Uses the preferred and fallback registries supplies at construction time, to
        /// get all values for a given key. Those in preferred registry are listed first, followed by those in fallback registry.
        /// </summary>
        /// <param name="key">key to find.</param>
        /// <returns>all values for key argument from preferred registry followed by all values
        /// for key argument in fallback registry</returns>
        public IEnumerable<object> GetAll(object key)
        {
            var collectionFromChild = _child.GetAll(key);
            var collectionFromParent = _parent.GetAll(key);
            return collectionFromChild.Concat(collectionFromParent);
        }
    }
}
