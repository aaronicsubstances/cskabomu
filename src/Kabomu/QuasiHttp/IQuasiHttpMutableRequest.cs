using Kabomu.QuasiHttp.EntityBody;
using System.Collections.Generic;

namespace Kabomu.QuasiHttp
{
    /// Provides convenient extension of<see cref="IQuasiHttpRequest"/> interface
    /// in which properties of the interface are mutable.
    public interface IQuasiHttpMutableRequest : IQuasiHttpRequest
    {
        new string Target { get; set; }

        new IDictionary<string, IList<string>> Headers { get; set; }

        new IQuasiHttpBody Body { get; set; }

        new string Method { get; set; }

        new string HttpVersion { get; set; }

        new IDictionary<string, object> Environment { get; set; }
    }
}