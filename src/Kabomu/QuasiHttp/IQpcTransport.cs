using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQpcTransport
    {
        void SendPdu(byte[] data, int offset, int length, string remoteEndpoint, Action<Exception> cb);
    }
}
