using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class IncomingTransfer : ITransfer
    {
        public ReceiveProtocol TransferProtocol { get; set; }
        public int RequestId { get; set; }
        public IncomingChunkTransferProtocol RequestBodyProtocol { get; set; }
        public OutgoingChunkTransferProtocol ResponseBodyProtocol { get; set; }
        public int TimeoutMillis { get; set; }
        public object RequestTimeoutId { get; set; }
        public STCancellationIndicator ApplicationProcessingCancellationIndicator { get; set; }
        public STCancellationIndicator SendResponseHeaderPduCancellationIndicator { get; set; }

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
