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
        int MaxPduPayloadSize { get; }

        bool SerializingEnabled { get; }
        void ProcessSendRequest(QuasiHttpRequestMessage request, object connectionHandleOrRemoteEndpoint,
            Action<Exception, QuasiHttpResponseMessage> cb);
        void SendPdu(QuasiHttpPdu pdu, object connectionHandleOrRemoteEndpoint,
            Action<Exception> cb);
        void SendSerializedPdu(byte[] data, int offset, int length, object connectionHandleOrRemoteEndpoint,
            Action<Exception> cb);
    }
}
