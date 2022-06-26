using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class SubsequentChunk
    {
        public byte Version { get; set; }
        public byte Flags { get; set; }
        public byte[] Data { get; set; }
        public int DataOffset { get; set; }
        public int DataLength { get; set; }

        public ByteBufferSlice[] Serialize()
        {
            var serialized = new ByteBufferSlice[DataLength > 0 ? 2 : 1];
            var dataPrefix = new byte[] { Version, Flags };
            serialized[0] = new ByteBufferSlice
            {
                Data = dataPrefix,
                Length = dataPrefix.Length
            };
            if (DataLength > 0)
            {
                serialized[1] = new ByteBufferSlice
                {
                    Data = Data,
                    Offset = DataOffset,
                    Length = DataLength
                };
            }
            return serialized;
        }

        public static SubsequentChunk Deserialize(byte[] data, int offset, int length)
        {
            if (length < 2)
            {
                throw new ArgumentException("too small to be a valid subsequent chunk");
            }
            var instance = new SubsequentChunk();
            instance.Version = data[offset];
            if (instance.Version == 0)
            {
                throw new ArgumentException("version not set");
            }
            instance.Flags = data[offset + 1];
            instance.Data = data;
            instance.DataOffset = offset + 2;
            instance.DataLength = length - 2;
            return instance;
        }
    }
}
