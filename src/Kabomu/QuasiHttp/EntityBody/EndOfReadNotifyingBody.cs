using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class EndOfReadNotifyingBody : IQuasiHttpBody
    {
        private readonly CancellationTokenSource _readCancellationHandle = new CancellationTokenSource();
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
            if (_readCancellationHandle.IsCancellationRequested)
            {
                return;
            }

            _readCancellationHandle.Cancel();
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
