using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
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

        public IQuasiHttpResponseHeaderPart Headers { get; set; }

        /// <summary>
        /// Gets or sets the response body.
        /// </summary>
        public object Body { get; set; }

        /// <summary>
        /// Gets or sets any objects which may be of interest to the code which
        /// will process a response.
        /// </summary>
        public IDictionary<string, object> Environment { get; set; }

        public Func<Task> Disposer { get; set; }
    }
}
