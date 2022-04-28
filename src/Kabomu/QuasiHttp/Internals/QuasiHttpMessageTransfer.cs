using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class QuasiHttpMessageTransfer
    {
        public QuasiHttpPdu Pdu { get; set; }
        public int TimeoutMillis { get; set; }
        public Action<Exception, QuasiHttpResponseMessage> RequestCallback { get; set; }
        public object RequestTimeoutId { get; set; }
        public STCancellationIndicator PendingResultCancellationIndicator { get; set; }
        public object ReplyConnectionHandle { get; set; }
    }
}
