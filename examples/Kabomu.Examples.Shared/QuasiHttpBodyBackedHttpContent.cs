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

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return IOUtils.CopyBytes(_backingBody.AsReader(), stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _backingBody.ContentLength;
            return _backingBody.ContentLength >= 0;
        }
    }
}