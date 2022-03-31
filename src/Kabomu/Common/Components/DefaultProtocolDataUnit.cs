using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Components
{
    public class DefaultProtocolDataUnit
    {
        public const byte PduTypeFirstChunk = 0x01;
        public const byte PduTypeFirstChunkAck = 0x02;
        public const byte PduTypeSubsequentChunk = 0x03;
        public const byte PduTypeSubsequentChunkAck = 0x04;
        public const byte Version01 = 0x01;

        public byte Version { get; set; }
        public byte PduType { get; set; }
        public long MessageId { get; set; }
        public byte Flags { get; set; }
        public byte ErrorCode { get; set; }
        public int DataOffset { get; set; }
        public int DataLength { get; set; }
        public byte[] Data { get; set; }

        public static bool IsStartedAtReceiverFlagPresent(byte flags)
        {
            return (flags & (1 << 7)) != 0;
        }

        public static bool IsHasMoreFlagPresent(byte flags)
        {
            return (flags & (1 << 6)) != 0;
        }

        public static byte ComputeFlags(bool startedAtReceiver, bool hasMore)
        {
            byte flags = 0;
            if (startedAtReceiver)
            {
                flags |= 1 << 7;
            }
            if (hasMore)
            {
                flags |= 1 << 6;
            }
            return flags;
        }
    }
}
