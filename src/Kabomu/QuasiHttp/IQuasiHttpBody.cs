using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpBody
    {
        string ContentType { get; }
        int ContentLength { get; }
        void OnDataRead(byte[] data, int offset, int bytesToRead, Action<Exception, int> cb);
        void OnEndRead(Exception e);
    }
}
