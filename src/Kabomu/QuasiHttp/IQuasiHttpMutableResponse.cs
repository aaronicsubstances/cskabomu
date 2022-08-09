using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Provides convenient extension of <see cref="IQuasiHttpResponse"/> interface
    /// in which properties of the interface are mutable.
    /// </summary>
    public interface IQuasiHttpMutableResponse : IQuasiHttpResponse
    {
        new bool StatusIndicatesSuccess { get; set; }

        new bool StatusIndicatesClientError { get;  set; }

        new string StatusMessage { get; set; }

        new IDictionary<string, IList<string>> Headers { get; set; }

        new IQuasiHttpBody Body { get; set; }

        new int HttpStatusCode { get; set; }

        new string HttpVersion { get; set; }
    }
}
