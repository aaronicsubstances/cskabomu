using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpBody
    {
        string ContentType { get; }
        int ContentLength { get; }
        string FilePath { get; }
        void OnDataRead(QuasiHttpBodyCallback cb);
        void OnEndRead(Exception error);
    }
}
