using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator
{
    public interface IRegistry
    {
        (bool present, object value) TryGet(string key);
        (bool present, object value) TryGet(Type key);
        IEnumerable<object> GetAll(string key);
        IEnumerable<object> GetAll(Type key);

        // The methods below are such that each can be composed
        // from TryGet() or GetAll() methods.

        object Get(string key);
        object Get(Type key);
        object GetFirstNonNull(string key, Func<object, object> transformFunction);
        object GetFirstNonNull(Type key, Func<object, object> transformFunction);
    }
}
