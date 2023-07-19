using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.QuasiHttp
{
    public class DelegatingCustomWriter : ICustomWriter
    {
        private readonly object _mutex = new object();
        private ICustomWriter? _writer;

        public ICustomWriter? BackingWriter
        {
            get
            {
                lock (_mutex)
                {
                    return _writer;
                }
            }
            set
            {
                lock (_mutex)
                {
                    _writer = value;
                }
            }
        }

        public async Task WriteBytes(byte[] data, int offset, int length)
        {
            var task = BackingWriter?.WriteBytes(data, offset, length);
            if (task != null)
            {
                await task;
            }
        }

        public async Task CustomDispose()
        {
            var task = BackingWriter?.CustomDispose();
            if (task != null)
            {
                await task;
            }
        }
    }
}
