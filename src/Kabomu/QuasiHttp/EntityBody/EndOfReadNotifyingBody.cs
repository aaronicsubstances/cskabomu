using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Wraps an instance of a quasi http body to provide a notification to clients after its EndOfRead() 
    /// method is called.
    /// </summary>
    public class EndOfReadNotifyingBody : IQuasiHttpBody
    {
        private readonly ICancellationHandle _readCancellationHandle = new DefaultCancellationHandle();
        private readonly IQuasiHttpBody _wrappedBody;
        private readonly Func<Task> _endOfReadCallback;

        public EndOfReadNotifyingBody(IQuasiHttpBody wrappedBody, Func<Task> endOfReadCallback)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentNullException(nameof(wrappedBody));
            }
            _wrappedBody = wrappedBody;
            _endOfReadCallback = endOfReadCallback;
        }

        public long ContentLength => _wrappedBody.ContentLength;
        public string ContentType => _wrappedBody.ContentType;

        public Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            return _wrappedBody.ReadBytes(data, offset, bytesToRead);
        }

        public async Task EndRead()
        {
            if (!_readCancellationHandle.Cancel())
            {
                return;
            }

            try
            {
                await _wrappedBody.EndRead();
            }
            finally
            {
                if (_endOfReadCallback != null)
                {
                    await _endOfReadCallback.Invoke();
                }
            }
        }
    }
}
