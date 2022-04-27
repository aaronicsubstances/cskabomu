using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpApplication
    {
        void ProcessPostRequest(QuasiHttpRequestMessage request, Action<Exception, object> cb);
        byte[] Serialize(object o);
        object Deserialize(IQuasiHttpBody body, string contentType, object serializationInfo);
    }
}
