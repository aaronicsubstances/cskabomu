using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.RequestParsing
{
    public interface IPathBinding
    {
        IDictionary<string, object> Tokens { get; }
        string Description { get; }
        string BoundPathPortion { get; }
        string UnboundPathPortion { get; }
    }
}
