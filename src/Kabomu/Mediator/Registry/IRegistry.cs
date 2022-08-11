using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public interface IRegistry
    {
        (bool, object) TryGet(object key);
        IEnumerable<object> GetAll(object key);

        // The methods below are such that each can be composed
        // from TryGet() or GetAll() methods.

        object Get(object key);
        (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction);
    }
}
