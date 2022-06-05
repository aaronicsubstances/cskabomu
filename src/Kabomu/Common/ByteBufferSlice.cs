using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public class ByteBufferSlice
    {
        public byte[] Data { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
    }
}
