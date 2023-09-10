using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
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
        private bool disposedValue;

        public DefaultQuasiHttpRequest()
        {
            Disposer = async () =>
            {
                if (Body != null)
                {
                    await Body.DisposeAsync();
                }
            };
        }

        public string Target { get; set; }
        public IDictionary<string, IList<string>> Headers { get; set; }
        public string HttpMethod { get; set; }
        public string HttpVersion { get; set; }
        public long ContentLength { get; set; }
        public Stream Body { get; set; }
        public IDictionary<string, object> Environment { get; set; }
        public Func<Task> Disposer { get; set; }

        public async ValueTask DisposeAsync()
        {
            if (Disposer != null)
            {
                await Disposer();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Disposer != null)
                    {
                        // don't wait.
                        _ = Disposer();
                    }
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                disposedValue = true;
            }
        }

        // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DefaultQuasiHttpResponse()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
