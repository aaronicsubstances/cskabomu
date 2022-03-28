using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Components
{
    public class DefaultMessageTransferOptions : IMessageTransferOptions
    {
        public int TimeoutMillis { get; set; }

        public ICancellationIndicator CancellationIndicator { get; set; }
    }
}
