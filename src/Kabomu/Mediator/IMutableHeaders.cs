using System.Collections.Generic;

namespace Kabomu.Mediator
{
    public interface IMutableHeaders : IHeaders
    {
        void Add(string name, string value);
        void Set(string name, string value);
        void Set(string name, IEnumerable<string> values);
        void Clear();
        void Remove(string name);
    }
}