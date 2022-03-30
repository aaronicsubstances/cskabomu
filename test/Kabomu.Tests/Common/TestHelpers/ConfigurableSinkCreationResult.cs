using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class ConfigurableSinkCreationResult
    {
        public int[] Delays { get; set; }
        public Exception DelayedError { get; set; }
        public IMessageSink Sink { get; set; }
        public ICancellationIndicator CancellationIndicator { get; set; }
        public Action<object, Exception> RecvCb { get; set; }
        public object RecvCbState { get; set; }
    }
}
