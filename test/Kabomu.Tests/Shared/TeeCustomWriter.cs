using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class TeeCustomWriter : ICustomWriter
    {
        private readonly object _mutex = new object();
        private List<ICustomWriter> _writers;

        public List<ICustomWriter> Writers
        {
            get
            {
                lock (_mutex)
                {
                    return _writers;
                }
            }
            set
            {
                lock (_mutex)
                {
                    _writers = value;
                }
            }
        }

        public async Task WriteBytes(byte[] data, int offset, int length)
        {
            var writers = Writers;
            if (writers == null) return;
            foreach (var writer in writers)
            {
                if (writer == null) continue;
                await writer.WriteBytes(data, offset, length);
            }
        }

        public async Task CustomDispose()
        {
            var writers = Writers;
            if (writers == null) return;
            foreach (var writer in writers)
            {
                if (writer == null) continue;
                await writer.CustomDispose();
            }
        }
    }
}
