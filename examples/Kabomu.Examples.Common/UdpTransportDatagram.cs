using Kabomu.Common;
using System;

namespace Kabomu.Examples.Common
{
    internal class UdpTransportDatagram
    {
        public static readonly byte Version01 = 1;
        public static readonly byte PduTypeRequest = 1;
        public static readonly byte PduTypeResponse = 2;

        public byte Version { get; set; }
        public byte PduType { get; set; }
        public byte Flags { get; set; }
        public int RequestId { get; set; }
        public byte[] Data { get; set; }
        public int DataOffset { get; set; }
        public int DataLength { get; set; }

        public static UdpTransportDatagram Deserialize(byte[] data, int offset, int length)
        {
            var pdu = new UdpTransportDatagram();
            pdu.Version = data[0];
            pdu.PduType = data[1];
            pdu.Flags = data[2];
            pdu.RequestId = (int)ByteUtils.DeserializeUpToInt64BigEndian(data, offset + 3, 4);
            pdu.Data = data;
            pdu.DataOffset = offset + 7;
            pdu.DataLength = length - 7;
            return pdu;
        }

        public byte[] Serialize()
        {
            var pduBytes = new byte[7 + DataLength];
            pduBytes[0] = Version;
            pduBytes[1] = PduType;
            pduBytes[2] = Flags;
            ByteUtils.SerializeUpToInt64BigEndian(RequestId, pduBytes, 3, 4);
            Array.Copy(Data, DataOffset, pduBytes, 7, DataLength);
            return pduBytes;
        }
    }
}