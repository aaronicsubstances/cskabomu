using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class OutgoingTransfer : ITransfer
    {
        public SendProtocol TransferProtocol { get; set; }
        public int RequestId { get; set; }
        public OutgoingChunkTransferProtocol RequestBodyProtocol { get; set; }
        public IncomingChunkTransferProtocol ResponseBodyProtocol { get; set; }
        public int TimeoutMillis { get; set; }
        public Action<Exception, QuasiHttpResponseMessage> RequestCallback { get; set; }
        public object RequestTimeoutId { get; set; }
        public STCancellationIndicator SendRequestHeaderPduCancellationIndicator { get; set; }

        public void Abort(Exception exception)
        {
            TransferProtocol.AbortTransfer(this, exception);
        }

        public void ResetTimeout()
        {
            TransferProtocol.ResetTimeout(this);
        }
    }
}
