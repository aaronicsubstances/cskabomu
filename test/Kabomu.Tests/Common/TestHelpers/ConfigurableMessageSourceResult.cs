using System;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class ConfigurableMessageSourceResult
    {
        public int[] Delays { get; set; }
        public Exception DelayedError { get; set; }
        public byte [] Data { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
        public object AdditionalPayload { get; set; }
        public bool HasMore { get; set; }
    }
}