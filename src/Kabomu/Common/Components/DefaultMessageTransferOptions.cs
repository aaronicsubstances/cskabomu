using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Components
{
    public class DefaultMessageTransferOptions : IMessageTransferOptions
    {
        public long MessageId { get; set; }

        public bool SendToExistingSink { get; set; }

        public int TimeoutMillis { get; set; }

        public ICancellationHandle CancellationHandle { get; set; }
    }
}
