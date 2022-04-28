using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpTransport
    {
        /// <summary>
        /// Memory-based transports return this property or null with a probability between 0 and 1,
        /// in order to catch any hidden errors during serialization to pdu.
        /// HTTP-based transports return this property as non-null always in order to completely take over
        /// processing of (Quasi) HTTP requests.
        /// </summary>
        IQuasiHttpApplication Application { get; }

        void SendPdu(byte[] data, int offset, int length, object connectionHandleOrRemoteEndpoint, Action<Exception> cb);
    }
}
