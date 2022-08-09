using System.Collections;

namespace Kabomu.Mediator
{
    public interface IMutableHeaders : IHeaders
    {
        void Add(string name, object value);
        void Set(string name, object value);
        void Set(string name, IEnumerable values);
        void Clear();
        void Remove(string name);
    }
}