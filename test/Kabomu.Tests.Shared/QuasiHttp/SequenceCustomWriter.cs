using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.QuasiHttp
{
    public class SequenceCustomWriter : ICustomWriter
    {
        private int _writerIndex;

        public IList<ICustomWriter>? Writers { get; set; }

        public async Task WriteBytes(byte[] data, int offset, int length)
        {
            var writers = Writers;
            if (writers != null)
            {
                await writers[_writerIndex].WriteBytes(data, offset, length);
            }
        }

        public void SwitchOver()
        {
            Interlocked.Increment(ref _writerIndex);
        }
    }
}
