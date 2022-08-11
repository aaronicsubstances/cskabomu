using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class EmptyRegistry : IRegistry
    {
        public static readonly IRegistry Instance = new EmptyRegistry();

        private EmptyRegistry()
        {
        }

        public (bool, object) TryGet(object key)
        {
            return (false, null);
        }

        public IEnumerable<object> GetAll(object key)
        {
            return Enumerable.Empty<object>();
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            return RegistryUtils.TryGetFirst(this, key, transformFunction);
        }

        public object Get(object key)
        {
            return RegistryUtils.Get(this, key);
        }
    }
}
