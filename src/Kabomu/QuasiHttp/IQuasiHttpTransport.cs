using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpTransport
    {
        int MaximumChunkSize { get; }
        bool IsChunkDeliveryAcknowledged { get; }
        void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb);
        void ReleaseConnection(object connection);
        void Write(object connection, byte[] data, int offset, int length,
            Action<Exception> cb);
        void Read(object connection, byte[] data, int offset, int length, 
            Action<Exception, int> cb);

        /// <summary>
        /// Memory-based transports return true with a probability between 0 and 1,
        /// in order to catch any hidden errors during serialization to pdu.
        /// HTTP-based transports return true always in order to completely take over
        /// processing of (Quasi) HTTP requests.
        /// </summary>
        bool DirectSendRequestProcessingEnabled { get; }
        void ProcessSendRequest(object remoteEndpoint, QuasiHttpRequestMessage request,
            Action<Exception, QuasiHttpResponseMessage> cb);
    }
}
