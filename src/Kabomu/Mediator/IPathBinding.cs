using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator
{
    public interface IPathBinding
    {
        IDictionary<string, string> Tokens { get; }
        string Description { get; }
        string BoundPathPortion { get; }
        string UnboundPathPortion { get; }
    }
}
