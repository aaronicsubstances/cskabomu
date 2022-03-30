using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class ConfigurableSendPduResult
    {
        public int[] Delays { get; set; }
        public Exception DelayedError { get; set; }
    }
}
