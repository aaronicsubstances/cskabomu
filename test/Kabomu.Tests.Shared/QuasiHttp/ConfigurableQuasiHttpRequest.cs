using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.QuasiHttp
{
    public class ConfigurableQuasiHttpRequest : IQuasiHttpMutableRequest
    {
        public string Target { get; set; }

        public IDictionary<string, IList<string>> Headers { get; set; }

        public IQuasiHttpBody Body { get; set; }

        public string Method { get; set; }

        public string HttpVersion { get; set; }

        public IDictionary<string, object> Environment { get; set; }

        public Func<Task> ReleaseFunc { get; set; }

        public Task Release()
        {
            return ReleaseFunc?.Invoke() ?? Task.CompletedTask;
        }
    }
}
