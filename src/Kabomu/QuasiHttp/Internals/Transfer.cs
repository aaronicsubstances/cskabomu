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
        public IChunkTransferProtocol MessageOrientedRequestBodyProtocol { get; set; }
        public IChunkTransferProtocol MessageOrientedResponseBodyProtocol { get; set; }
        public Action<Exception, QuasiHttpResponseMessage> SendCallback { get; set; }
        public STCancellationIndicator ProcessingCancellationIndicator { get; set; }
        public bool RequestBodyTransferRequired { get; set; }
        public bool ResponseBodyTransferRequired { get; set; }
    }
}
