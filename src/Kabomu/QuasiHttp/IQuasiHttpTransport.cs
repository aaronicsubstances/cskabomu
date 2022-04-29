using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpTransport
    {
        /// <summary>
        /// Memory-based transports return true with a probability between 0 and 1,
        /// in order to catch any hidden errors during serialization to pdu.
        /// HTTP-based transports return true always in order to completely take over
        /// processing of (Quasi) HTTP requests.
        /// </summary>
        bool DirectSendRequestProcessingEnabled { get; }
        void ProcessSendRequest(QuasiHttpRequestMessage request, Action<Exception, QuasiHttpResponseMessage> cb);
        void SendPdu(byte[] data, int offset, int length, object connectionHandleOrRemoteEndpoint, Action<Exception> cb);
    }
}
