using System.Collections.Generic;

namespace Kabomu.Mediator
{
    public interface IHeaders
    {
        string Get(string name);
        IDictionary<string, List<string>> MakeCopy();
    }
}