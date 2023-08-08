using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.Common
{
    public class DemoSimpleCustomWritable : ICustomWritable
    {
        private readonly byte[] _srcData;

        public DemoSimpleCustomWritable() :
            this(null)
        {
        }

        public DemoSimpleCustomWritable(byte[] srcData)
        {
            _srcData = srcData ?? new byte[0];
        }

        public Task WriteBytesTo(object writer)
        {
            return IOUtils.WriteBytes(writer, _srcData, 0, _srcData.Length);
        }
    }
}
