using System;

namespace Kabomu.Mediator.Path
{
    internal class DefaultPathToken
    {
        public const int TokenTypeLiteral = 1;

        public const int TokenTypeSegment = 2;

        public const int TokenTypeWildCard = 3;

        public readonly int Type;

        public DefaultPathToken(DefaultPathToken src, int overridingType)
        {
            Type = overridingType;
            if (src == null)
            {
                SampleIndexOfValue = -1;
            }
            else
            {
                Value = src.Value;
                SampleIndexOfValue = src.SampleIndexOfValue;
                EmptySegmentAllowed = src.EmptySegmentAllowed;
            }
        }

        public string Value { get; private set; }
        public int SampleIndexOfValue { get; private set; }
        public bool EmptySegmentAllowed { get; private set; }

        public void Update(int sampleIndex, string value)
        {
            if (value == "")
            {
                EmptySegmentAllowed = true;
            }
            bool proceedWithUpdate = false;
            if (SampleIndexOfValue == -1)
            {
                // first time.
                proceedWithUpdate = true;
            }
            else if (Value == "" && value != "")
            {
                // always allow non empty values to override empty ones
                // even if non empty value appears after empty one in
                // sample.
                proceedWithUpdate = true;
            }
            else if (sampleIndex < SampleIndexOfValue)
            {
                // only update if incoming sample index precedes the existing sample
                // in original submission.
                proceedWithUpdate = true;
            }

            if (proceedWithUpdate)
            {
                SampleIndexOfValue = sampleIndex;
                Value = value;
            }
        }
    }
}
