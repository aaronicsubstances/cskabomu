using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public interface IQuasiHttpTransport
    {
        int MaxChunkSize { get; }
        public UncaughtErrorCallback ErrorHandler { get; set; }
        void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb);
        void OnReleaseConnection(object connection);
        void WriteBytes(object connection, byte[] data, int offset, int length,
            Action<Exception> cb);
        void ReadBytes(object connection, byte[] data, int offset, int length, 
            Action<Exception, int> cb);

        /// <summary>
        /// Memory-based transports return true with a probability between 0 and 1,
        /// in order to catch any hidden errors during serialization to bytes.
        /// HTTP-based transports return true always in order to completely take over
        /// processing of (Quasi) HTTP requests.
        /// </summary>
        bool DirectSendRequestProcessingEnabled { get; }

        void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequest request,
            Action<Exception, IQuasiHttpResponse> cb);
    }
}
