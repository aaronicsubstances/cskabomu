using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class EmptyRegistry : IRegistry
    {
        public static readonly IRegistry Instance = new EmptyRegistry();

        private readonly IEnumerable<object> _getAllRetVal = new object[0];

        private EmptyRegistry()
        {
        }

        public (bool, object) TryGet(object key)
        {
            return (false, null);
        }

        public object Get(object key)
        {
            throw new NotInRegistryException(key);
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            return (false, null);
        }

        public IEnumerable<object> GetAll(object key)
        {
            return _getAllRetVal;
        }
    }
}
