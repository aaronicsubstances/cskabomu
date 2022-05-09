using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class Transfer
    {
        public object Connection { get; set; }
        public int TimeoutMillis { get; set; }
        public object TimeoutId { get; set; }
        public IChunkTransferProtocol OutgoingRequestBodyProtocol { get; set; }
        public IChunkTransferProtocol IncomingRequestBodyProtocol { get; set; }
        public IChunkTransferProtocol OutgoingResponseBodyProtocol { get; set; }
        public IChunkTransferProtocol IncomingResponseBodyProtocol { get; set; }
        public Action<Exception, QuasiHttpResponseMessage> RequestCallback { get; set; }
        public STCancellationIndicator ApplicationProcessingCancellationIndicator { get; set; }
        public STCancellationIndicator SendRequestCancellationIndicator { get; set; }
        public STCancellationIndicator SendResponseCancellationIndicator { get; set; }
    }
}
