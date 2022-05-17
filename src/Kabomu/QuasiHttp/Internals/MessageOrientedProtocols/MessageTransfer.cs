using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals.MessageOrientedProtocols
{
    internal class MessageTransfer
    {
        public object Connection { get; set; }
        public int TimeoutMillis { get; set; }
        public object TimeoutId { get; set; }
        public IChunkTransferProtocol RequestBodyProtocol { get; set; }
        public IChunkTransferProtocol ResponseBodyProtocol { get; set; }
        public Action<Exception, QuasiHttpResponseMessage> SendCallback { get; set; }
        public STCancellationIndicator ProcessingCancellationIndicator { get; set; }
    }
}
