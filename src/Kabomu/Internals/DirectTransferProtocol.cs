using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class DirectTransferProtocol : ITransferProtocol
    {
        public IParentTransferProtocol Parent { get; set; }
        public object Connection { get; set; }
        public STCancellationIndicator ProcessingCancellationIndicator { get; set; }
        public int TimeoutMillis { get; set; }
        public object TimeoutId { get; set; }
        public Action<Exception, IQuasiHttpResponse> SendCallback { get; set; }

        public void Cancel(Exception e)
        {
            // nothing to do.
        }

        public void OnReceive()
        {
            throw new NotImplementedException();
        }

        public void OnReceiveMessage(byte[] data, int offset, int length)
        {
            throw new NotImplementedException();
        }

        public void OnSend(IQuasiHttpRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
