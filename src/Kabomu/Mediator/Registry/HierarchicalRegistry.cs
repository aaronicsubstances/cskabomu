using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class HierarchicalRegistry : IRegistry
    {
        private readonly IRegistry _parent;
        private readonly IRegistry _child;

        public HierarchicalRegistry(IRegistry parent, IRegistry child)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _child = child ?? throw new ArgumentNullException(nameof(child));
        }

        public (bool, object) TryGet(object key)
        {
            var (present, value) = _child.TryGet(key);
            if (present)
            {
                return (present, value);
            }
            return _parent.TryGet(key);
        }

        public IEnumerable<object> GetAll(object key)
        {
            var collectionFromChild = _child.GetAll(key);
            var collectionFromParent = _parent.GetAll(key);
            return collectionFromChild.Concat(collectionFromParent);
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            var result = _child.TryGetFirst(key, transformFunction);
            if (result.Item1)
            {
                return result;
            }
            return _parent.TryGetFirst(key, transformFunction);
        }

        public object Get(object key)
        {
            return RegistryUtils.Get(this, key);
        }
    }
}
