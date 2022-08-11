using System.Collections.Generic;

namespace Kabomu.Mediator.Handling
{
    public interface IMutableHeaders : IHeaders
    {
        IMutableHeaders Add(string name, string value);
        IMutableHeaders Set(string name, string value);
        IMutableHeaders Set(string name, IEnumerable<string> values);
        IMutableHeaders Clear();
        IMutableHeaders Remove(string name);
    }
}