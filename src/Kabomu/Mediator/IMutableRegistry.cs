using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator
{
    public interface IMutableRegistry : IRegistry
    {
        Task Add(string key, object value);
        Task Add(Type t, object value);
        Task AddByType<T>(T value);
        Task AddLazy(string key, Func<Task<object>> valueGenerator);
        Task AddLazy(Type t, Func<Task<object>> valueGenerator);
        Task AddLazyByType<T>(Func<Task<T>> valueGenerator);
        Task<bool> TryRemove(string key);
        Task<bool> TryRemove(Type t);
        Task<bool> TryRemoveByType<T>();
    }
}
