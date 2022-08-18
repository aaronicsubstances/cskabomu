using System;

namespace Kabomu.Mediator.Path
{
    internal class DefaultPathToken
    {
        public const int TokenTypeLiteral = 1;

        public const int TokenTypeSegment = 2;

        public const int TokenTypeWildCard = 3;

        public int Type;
        public string Value;
        public int SampleIndexOfValue = -1;
        public bool EmptySegmentAllowed;
    }
}
