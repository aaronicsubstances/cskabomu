using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.QuasiHttp
{
    public class SequenceCustomReader : ICustomReader
    {
        public List<object>? Readers { get; set; }

        public async Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            var readers = Readers;
            if (readers == null) return 0;
            int bytesRead = 0;
            foreach (var reader in readers)
            {
                bytesRead = await IOUtils.ReadBytes(reader,
                    data, offset, length);
                if (bytesRead > 0)
                {
                    break;
                }
            }
            return bytesRead;
        }
    }
}
