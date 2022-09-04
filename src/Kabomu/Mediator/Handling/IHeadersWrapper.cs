using System.Collections.Generic;

namespace Kabomu.Mediator.Handling
{
    public interface IHeadersWrapper
    {
        string Get(string name);
        IEnumerable<string> GetAll(string name);
        ICollection<string> GetNames();
    }
}