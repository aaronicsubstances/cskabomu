using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class CustomReaderBackedBody : AbstractQuasiHttpBody, ICustomReader
    {
        private readonly ICustomReader _backingReader;

        public CustomReaderBackedBody(ICustomReader wrappedReader)
        {
            if (wrappedReader == null)
            {
                throw new ArgumentNullException(nameof(wrappedReader));
            }
            _backingReader = wrappedReader;
        }

        public override Task CustomDispose() => _backingReader.CustomDispose();

        public Task<int> ReadBytes(byte[] data, int offset, int length)
            => _backingReader.ReadBytes(data, offset, length);
    }
}
