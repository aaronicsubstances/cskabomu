using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public interface IMutableRegistry : IRegistry
    {
        IMutableRegistry Add(object key, object value);
        IMutableRegistry AddGenerator(object key, Func<object> valueGenerator);
        IMutableRegistry Remove(object key);
    }
}
