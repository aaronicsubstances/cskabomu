using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator
{
    public interface IMutableRegistry : IRegistry
    {
        void Add(string key, object value);
        void Add(Type key, object value);
        void AddLazy(string key, Func<object> valueGenerator);
        void AddLazy(Type key, Func<object> valueGenerator);
        bool Remove(string key);
        bool Remove(Type key);
    }
}
