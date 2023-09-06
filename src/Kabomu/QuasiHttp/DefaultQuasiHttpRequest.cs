using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Provides convenient implementation of <see cref="IQuasiHttpRequest"/>
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

        public IQuasiHttpRequestHeaderPart Headers { get; set; }

        /// <summary>
        /// Gets or sets the request body.
        /// </summary>
        public object Body { get; set; }

        /// Gets or sets any objects which may be of interest to code
        /// which will process a request.
        public IDictionary<string, object> Environment { get; set; }

        public Func<Task> Disposer { get; set; }
    }
}
