using Kabomu.Common.Abstractions;
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
        public ITransferEndpoint RemoteEndpoint { get; set; }

        /// <summary>
        /// implement this to improve performance of deserializing remote endpoints and also to
        /// enable creation of endpoints belonging to classes other than DefaultTransferEndpoint class.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="knownRemoteEndpoints"></param>
        /// <returns></returns>
        public static ITransferEndpoint DeserializeRemoteEndpoint(byte[] data, int offset, int length,
            List<ITransferEndpoint> knownRemoteEndpoints)
        {
            throw new NotImplementedException();
        }

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
