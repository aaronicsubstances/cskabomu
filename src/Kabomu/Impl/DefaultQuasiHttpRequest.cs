using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Impl
{
    /// <summary>
    /// Provides default implementation of <see cref="IQuasiHttpRequest"/>
    /// interface.
    /// </summary>
    public class DefaultQuasiHttpRequest : IQuasiHttpRequest
    {
        /// <summary>
        /// Creates a new instance with the <see cref="Disposer"/>
        /// property initialized to a function which tries to
        /// dispose off <see cref="Body"/> property if it implements the
        /// <see cref="ICustomDisposable"/> interface.
        /// </summary>
        public DefaultQuasiHttpRequest()
        {
            Disposer = async () =>
            {
                if (Body is ICustomDisposable disposable)
                {
                    var disposer = disposable.Disposer;
                    if (disposer != null)
                    {
                        await disposer();
                    }
                }
            };
        }

        public string Target { get; set; }
        public IDictionary<string, IList<string>> Headers { get; set; }
        public string HttpMethod { get; set; }
        public string HttpVersion { get; set; }
        public long ContentLength { get; set; }
        public object Body { get; set; }
        public IDictionary<string, object> Environment { get; set; }
        public Func<Task> Disposer { get; set; }
    }
}
