using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class IncomingTransfer
    {
        public int RequestId { get; set; }
        public QuasiHttpRequestMessage Request { get; set; }
        public QuasiHttpResponseMessage Response { get; set; }
        public int TimeoutMillis { get; set; }
        public object RequestTimeoutId { get; set; }
        public object ReplyConnectionHandle { get; set; }
        public STCancellationIndicator ApplicationProcessingCancellationIndicator { get; set; }
        public STCancellationIndicator SendPduCancellationIndicator { get; set; }
        public bool RequestBodyTransferRequired { get; set; }
        public bool ResponseBodyTransferRequired { get; set; }
    }
}
