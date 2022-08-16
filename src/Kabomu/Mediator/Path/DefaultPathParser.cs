using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Path
{
    public static class DefaultPathParser
    {
        public static IPathMatcher Parse(string path)
        {
            throw new NotImplementedException();
        }

        internal class DefaultPathMatcher : IPathMatcher
        {
            public IPathMatchResult Match(string relativePath)
            {
                throw new NotImplementedException();
            }
        }
    }
}
