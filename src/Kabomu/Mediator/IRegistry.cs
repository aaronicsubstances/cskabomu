using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator
{
    public interface IRegistry
    {
        Task<T> Get<T>(string key);
        Task<object> Get(Type t);
        Task<T> GetByType<T>();
        Task<T> TryGet<T>(string key, T defaultValue);
        Task<object> TryGet(Type t, object defaultValue);
        Task<T> TryGetByType<T>(T defaultValue);
        Task<T> GetFirst<T>(string key);
        Task<object> GetFirst(Type t);
        Task<T> GetFirstByType<T>();
        Task<IEnumerable<T>> GetAll<T>(string key);
        Task<IEnumerable> GetAll(Type t);
        Task<IEnumerable<T>> GetAllByType<T>();
    }
}
