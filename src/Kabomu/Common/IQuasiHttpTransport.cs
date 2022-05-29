using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public interface IQuasiHttpTransport
    {
        int MaxMessageOrChunkSize { get; }
        bool IsByteOriented { get; }
        void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb);
        void ReleaseConnection(object connection);
        void WriteBytes(object connection, byte[] data, int offset, int length,
            Action<Exception> cb);
        void SendMessage(object connection, byte[] data, int offset, int length,
            Action<Action<bool>> cancellationEnquirer, Action<Exception> cb);
        void ReadBytes(object connection, byte[] data, int offset, int length, 
            Action<Exception, int> cb);

        /// <summary>
        /// Memory-based transports return true with a probability between 0 and 1,
        /// in order to catch any hidden errors during serialization to bytes.
        /// HTTP-based transports return true always in order to completely take over
        /// processing of (Quasi) HTTP requests.
        /// </summary>
        bool DirectSendRequestProcessingEnabled { get; }

        void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequestMessage request,
            Action<Exception, IQuasiHttpResponseMessage> cb);
    }
}
