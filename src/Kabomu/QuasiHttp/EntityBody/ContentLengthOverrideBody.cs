using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class ContentLengthOverrideBody : IQuasiHttpBody
    {
        private readonly IQuasiHttpBody _wrappedBody;
        private long _bytesRemaining;

        public ContentLengthOverrideBody(IQuasiHttpBody wrappedBody, long contentLength)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentNullException(nameof(wrappedBody));
            }
            _wrappedBody = wrappedBody;
            ContentLength = contentLength;
            if (ContentLength >= 0)
            {
                _bytesRemaining = contentLength;
            }
            else
            {
                _bytesRemaining = -1;
            }
        }

        public long ContentLength { get; }
        public string ContentType => _wrappedBody.ContentType;

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (_bytesRemaining >= 0)
            {
                bytesToRead = (int)Math.Min(bytesToRead, _bytesRemaining);
            }

            // even if bytes to read is zero at this stage, still go ahead and call
            // wrapped body instead of trying to optimize by returning zero, so that
            // any end of read error can be thrown.
            int bytesRead = await _wrappedBody.ReadBytes(data, offset, bytesToRead);

            if (_bytesRemaining > 0)
            {
                if (bytesRead == 0)
                {
                    var e = new Exception($"could not read remaining {_bytesRemaining} " +
                        $"bytes before end of read");
                    throw e;
                }
                _bytesRemaining -= bytesRead;
            }
            return bytesRead;
        }

        public Task EndRead()
        {
            return _wrappedBody.EndRead();
        }
    }
}
