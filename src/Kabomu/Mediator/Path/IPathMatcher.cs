using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Path
{
    public interface IPathMatcher
    {
        IPathMatchResult Match(string relativePath);
    }
}
