using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Wraps an instance of a quasi http body to ensure that its methods are called within locks (actually
    /// any mutex api).
    /// </summary>
    public class SynchronizedBody : IQuasiHttpBody
    {
        private readonly IQuasiHttpBody _wrappedBody;

        /// <summary>
        /// Creates a new instance, with a lock-based mutex api as the initial means of synchronization.
        /// </summary>
        /// <param name="wrappedBody">the quasi http instance whose method calls will be synchronized.</param>
        public SynchronizedBody(IQuasiHttpBody wrappedBody)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentNullException(nameof(wrappedBody));
            }
            _wrappedBody = wrappedBody;
            MutexApi = new LockBasedMutexApi();
        }

        /// <summary>
        /// Gets or sets the mutex api instance for ensuring safe shared accesses to the quasi http body
        /// being protected, ie the instance provided at construction time.
        /// </summary>
        public IMutexApi MutexApi { get; set; }

        /// <summary>
        /// Same as the content length of the quasi body instance whose reads are being synchronized,
        /// ie the instance provided at construction time.
        /// </summary>
        public long ContentLength => _wrappedBody.ContentLength;

        /// <summary>
        /// Same as the content type of the body instance whose reads are being synchronized,
        /// ie the instance provided at construction time.
        /// </summary>
        public string ContentType => _wrappedBody.ContentType;

        public Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            return _wrappedBody.ReadBytes(data, offset, bytesToRead);
        }

        public Task EndRead()
        {
            return _wrappedBody.EndRead();
        }
    }
}
