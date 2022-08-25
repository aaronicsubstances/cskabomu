using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Path
{
    internal class DefaultPathMatchResultInternal : IPathMatchResult
    {
        public DefaultPathMatchResultInternal()
        {
        }

        public string BoundPath { get; set; }

        public string UnboundRequestTarget { get; set; }

        public IDictionary<string, string> PathValues { get; set; }
    }
}
