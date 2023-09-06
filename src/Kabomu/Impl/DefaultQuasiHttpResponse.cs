using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Impl
{
    /// <summary>
    /// Provides default implementation of <see cref="IQuasiHttpResponse"/>
    /// interface.
    /// </summary>
    public class DefaultQuasiHttpResponse : IQuasiHttpResponse
    {
        /// <summary>
        /// Creates a new instance with the <see cref="Disposer"/>
        /// property initialized to a function which tries to
        /// dispose off <see cref="Body"/> property if it implements the
        /// <see cref="ICustomDisposable"/> interface.
        /// </summary>
        public DefaultQuasiHttpResponse()
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

        public int StatusCode { get; set; }
        public IDictionary<string, IList<string>> Headers { get; set; }
        public string HttpStatusMessage { get; set; }
        public string HttpVersion { get; set; }
        public long ContentLength { get; set; }
        public object Body { get; set; }
        public IDictionary<string, object> Environment { get; set; }
        public Func<Task> Disposer { get; set; }
    }
}
