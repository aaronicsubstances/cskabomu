using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpBody
    {
        string ContentType { get; }
        int ContentLength { get; }
        void OnDataRead(QuasiHttpBodyCallback cb);
        void Close();
    }
}
