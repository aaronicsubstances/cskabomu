using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Path
{
    internal class DefaultPathMatchResult : IPathMatchResult
    {
        public DefaultPathMatchResult()
        {
        }

        public string BoundPathPortion { get; set; }

        public string UnboundPathPortion { get; set; }

        public IDictionary<string, string> PathValues { get; set; }
    }
}
