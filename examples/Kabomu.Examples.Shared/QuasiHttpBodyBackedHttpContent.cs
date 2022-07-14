using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class QuasiHttpBodyBackedHttpContent : HttpContent
    {
        private readonly IQuasiHttpBody _backingBody;

        public QuasiHttpBodyBackedHttpContent(IQuasiHttpBody backingBody)
        {
            _backingBody = backingBody;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var data = new byte[TransportUtils.DefaultMaxChunkSize];
            while (true)
            {
                var bytesRead = await _backingBody.ReadBytes(data, 0, data.Length);
                if (bytesRead == 0)
                {
                    break;
                }
                await stream.WriteAsync(data, 0, bytesRead);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _backingBody.ContentLength;
            return _backingBody.ContentLength >= 0;
        }
    }
}