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
        public bool RequestBodyTransferCompleted { get; set; }
        public bool ResponseBodyTransferCompleted { get; set; }
        public STCancellationIndicator ApplicationProcessingCancellationIndicator { get; set; }
        public STCancellationIndicator SendResponseHeaderPduCancellationIndicator { get; set; }
        public STCancellationIndicator SendRequestBodyPduCancellationIndicator { get; set; }
        public STCancellationIndicator SendResponseBodyPduCancellationIndicator { get; set; }
        public STCancellationIndicator ResponseBodyCallbackCancellationIndicator { get; set; }
    }
}
