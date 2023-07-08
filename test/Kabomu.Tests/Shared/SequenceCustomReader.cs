using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class SequenceCustomReader : ICustomReader
    {
        public List<ICustomReader> Readers { get; set; }

        public async Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            var readers = Readers;
            if (readers == null) return 0;
            int bytesRead = 0;
            foreach (var reader in Readers)
            {
                if (reader == null) continue;
                bytesRead = await reader.ReadBytes(data, offset, length);
                if (bytesRead > 0)
                {
                    break;
                }
            }
            return bytesRead;
        }

        public async Task CustomDispose()
        {
            var readers = Readers;
            if (readers == null) return;
            foreach (var reader in Readers)
            {
                if (reader == null) continue;
                await reader.CustomDispose();
            }
        }
    }
}
