using System;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class ConfigurableMessageSinkResult
    {
        public int[] Delays { get; set; }
        public Exception DelayedError { get; set; }
    }
}