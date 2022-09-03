using System.Collections.Generic;

namespace Kabomu.Mediator.Handling
{
    public interface IMutableHeadersWrapper : IHeadersWrapper
    {
        IMutableHeadersWrapper Add(string name, string value);
        IMutableHeadersWrapper Set(string name, string value);
        IMutableHeadersWrapper Set(string name, IEnumerable<string> values);
        IMutableHeadersWrapper Clear();
        IMutableHeadersWrapper Remove(string name);
    }
}