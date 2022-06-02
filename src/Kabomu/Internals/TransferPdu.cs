using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    public class TransferPdu
    {
        public const byte Version01 = 1;

        public const byte PduTypeRequest = 1;
        public const byte PduTypeResponse = 2;
        public const byte PduTypeRequestChunkGet = 3;
        public const byte PduTypeResponseChunkGet = 4;
        public const byte PduTypeRequestChunkRet = 5;
        public const byte PduTypeResponseChunkRet = 6;

        public byte Version { get; set; }
        public byte PduType { get; set; }
        public byte Flags { get; set; }
        public int SequenceNumber { get; set; }
        public string Path { get; set; }
        public bool StatusIndicatesSuccess { get; set; }
        public bool StatusIndicatesClientError { get; set; }
        public string StatusMessage { get; set; }
        public int ContentLength { get; set; }
        public string ContentType { get; set; }
        public Dictionary<string, List<string>> Headers { get; set; }
        public byte[] Data { get; set; }
        public int DataOffset { get; set; }
        public int DataLength{ get; set; }

        public static TransferPdu Deserialize(byte[] data, int offset, int length)
        {
            if (length < 11)
            {
                throw new ArgumentException("too small to be a valid pdu");
            }

            var pdu = new TransferPdu();
            
            pdu.Version = (byte)ByteUtils.DeserializeUpToInt64BigEndian(data, offset, 1);
            pdu.PduType = (byte)ByteUtils.DeserializeUpToInt64BigEndian(data, offset + 1, 1);
            pdu.Flags = (byte)ByteUtils.DeserializeUpToInt64BigEndian(data, offset + 2, 1);
            pdu.SequenceNumber = (int)ByteUtils.DeserializeUpToInt64BigEndian(data, offset + 3, 4);

            var csvDataLength = (int)ByteUtils.DeserializeUpToInt64BigEndian(data, offset + 7, 4);
            if (csvDataLength + 11 > length)
            {
                throw new ArgumentException("invalid pdu");
            }
            var csv = ByteUtils.BytesToString(data, offset + 11, csvDataLength);
            var csvData = CsvUtils.Deserialize(csv);
            if (csvData[0].Count > 0)
            {
                pdu.Path = csvData[0][0];
            }
            pdu.StatusIndicatesSuccess = bool.Parse(csvData[1][0]);
            pdu.StatusIndicatesClientError = bool.Parse(csvData[2][0]);
            if (csvData[3].Count > 0)
            {
                pdu.StatusMessage = csvData[3][0];
            }
            pdu.ContentLength = int.Parse(csvData[4][0]);
            if (csvData[5].Count > 0)
            {
                pdu.ContentType = csvData[5][0];
            }
            for (int i = 6; i < csvData.Count; i++)
            {
                var headerRow = csvData[i];
                if (headerRow.Count < 2)
                {
                    continue;
                }
                var headerValue = new List<string>(headerRow.GetRange(1, headerRow.Count - 1));
                if (pdu.Headers == null)
                {
                    pdu.Headers = new Dictionary<string, List<string>>();
                }
                pdu.Headers.Add(headerRow[0], headerValue);
            }

            pdu.Data = data;
            pdu.DataOffset = offset + 11 + csvDataLength;
            pdu.DataLength = length - 11 - csvDataLength;

            return pdu;
        }

        public byte[] Serialize()
        {
            var csvData = new List<List<string>>();
            csvData.Add(Path != null ? new List<string> { Path } : new List<string>());
            csvData.Add(new List<string> { StatusIndicatesSuccess.ToString() });
            csvData.Add(new List<string> { StatusIndicatesClientError.ToString() });
            csvData.Add(StatusMessage != null ? new List<string> { StatusMessage } : new List<string>());
            csvData.Add(new List<string> { ContentLength.ToString() });
            csvData.Add(ContentType != null ? new List<string> { ContentType } : new List<string>());
            if (Headers != null)
            {
                foreach (var header in Headers)
                {
                    if (header.Value.Count == 0)
                    {
                        continue;
                    }
                    var headerRow = new List<string> { header.Key };
                    headerRow.AddRange(header.Value);
                    csvData.Add(headerRow);
                }
            }
            var csv = CsvUtils.Serialize(csvData);
            var csvBytes = ByteUtils.StringToBytes(csv);
            var lengthOfBinaryBytes = 15 + DataLength;
            var pduBytes = new byte[lengthOfBinaryBytes + csvBytes.Length];
            int offset = 0;
            ByteUtils.SerializeUpToInt64BigEndian(pduBytes.Length - 4, pduBytes, 0, 4);
            offset += 4;
            ByteUtils.SerializeUpToInt64BigEndian(Version, pduBytes, offset, 1);
            offset += 1;
            ByteUtils.SerializeUpToInt64BigEndian(PduType, pduBytes, offset, 1);
            offset += 1;
            ByteUtils.SerializeUpToInt64BigEndian(Flags, pduBytes, offset, 1);
            offset += 1;
            ByteUtils.SerializeUpToInt64BigEndian(SequenceNumber, pduBytes, offset, 4);
            offset += 4;

            ByteUtils.SerializeUpToInt64BigEndian(csvBytes.Length, pduBytes, offset, 4);
            offset += 4;
            Array.Copy(csvBytes, 0, pduBytes, offset, csvBytes.Length);
            offset += csvBytes.Length;

            if (DataLength > 0)
            {
                Array.Copy(Data, DataOffset, pduBytes, offset, DataLength);
                offset += DataLength;
            }

            if (offset != pduBytes.Length)
            {
                throw new Exception("serialization algorithm error");
            }
            return pduBytes;
        }
    }
}
