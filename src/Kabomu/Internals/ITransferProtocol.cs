using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal interface ITransferProtocol
    {
        IParentTransferProtocol Parent { get; set; }
        object Connection { get; set; }
        STCancellationIndicator ProcessingCancellationIndicator { get; set; }
        int TimeoutMillis { get; set; }
        object TimeoutId { get; set; }
        Action<Exception, IQuasiHttpResponse> SendCallback { get; set; }

        void Cancel(Exception e);
        void OnSend(IQuasiHttpRequest request);
        void OnReceive();
        void OnReceiveMessage(byte[] data, int offset, int length);
    }
}
