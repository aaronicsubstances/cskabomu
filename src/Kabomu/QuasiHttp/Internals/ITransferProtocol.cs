using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal interface ITransferProtocol
    {
        IParentTransferProtocol Parent { get; set; }
        object Connection { get; set; }
        STCancellationIndicator ProcessingCancellationIndicator { get; set; }
        int TimeoutMillis { get; set; }
        object TimeoutId { get; set; }
        Action<Exception, QuasiHttpResponseMessage> SendCallback { get; set; }

        void Cancel(Exception e);
        void OnSend(QuasiHttpRequestMessage request);
        void OnReceive();
        void OnReceiveMessage(byte[] data, int offset, int length);
    }
}
