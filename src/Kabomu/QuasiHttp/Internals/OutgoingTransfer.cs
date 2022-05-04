using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class OutgoingTransfer
    {
        public int RequestId { get; set; }
        public QuasiHttpRequestMessage Request { get; set; }
        public QuasiHttpResponseMessage Response { get; set; }
        public int TimeoutMillis { get; set; }
        public Action<Exception, QuasiHttpResponseMessage> RequestCallback { get; set; }
        public object RequestTimeoutId { get; set; }
        public object ReplyConnectionHandle { get; set; }
        public bool RequestBodyTransferCompleted { get; set; }
        public bool ResponseBodyTransferCompleted { get; set; }
        public STCancellationIndicator DirectRequestProcessingCancellationIndicator { get; set; }
        public STCancellationIndicator SendRequestHeaderPduCancellationIndicator { get; set; }
        public STCancellationIndicator SendRequestBodyPduCancellationIndicator { get; set; }
        public STCancellationIndicator SendResponseBodyPduCancellationIndicator { get; set; }
        public STCancellationIndicator RequestBodyCallbackCancellationIndicator { get; set; }
    }
}
