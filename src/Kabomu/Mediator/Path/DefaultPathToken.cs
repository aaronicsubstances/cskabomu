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
        public bool EmptySegmentAllowed = false;

        public void UpdateValue(int sampleIndex, string value)
        {
            // only update if incoming sample index precedes the existing sample
            // in original submission.
            if (sampleIndex < SampleIndexOfValue)
            {
                SampleIndexOfValue = sampleIndex;
                Value = value;
            }
        }
    }
}