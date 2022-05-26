﻿using Kabomu.Common;
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
        public const byte PduTypeFin = 3;
        public const byte PduTypeRequestChunkGet = 4;
        public const byte PduTypeRequestChunkRet = 5;
        public const byte PduTypeResponseChunkGet = 6;
        public const byte PduTypeResponseChunkRet = 7;

        public byte Version { get; set; }
        public byte PduType { get; set; }
        public byte Flags { get; set; }
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
        public bool IncludeLengthPrefixDuringSerialization { get; set; }

        public static TransferPdu Deserialize(byte[] data, int offset, int length)
        {
            var pdu = new TransferPdu();

            var csv = ByteUtils.BytesToString(data, offset, length);
            var csvData = CsvUtils.Deserialize(csv);
            pdu.Version = byte.Parse(csvData[0][0]);
            pdu.PduType = byte.Parse(csvData[1][0]);
            pdu.Flags = byte.Parse(csvData[2][0]);
            pdu.Path = csvData[3][0];
            pdu.StatusIndicatesSuccess = bool.Parse(csvData[4][0]);
            pdu.StatusIndicatesClientError = bool.Parse(csvData[5][0]);
            pdu.StatusMessage = csvData[6][0];
            pdu.ContentLength = int.Parse(csvData[7][0]);
            pdu.ContentType = csvData[8][0];
            pdu.Data = Convert.FromBase64String(csvData[9][0]);
            pdu.DataLength = pdu.Data.Length;
            for (int i = 10; i < csvData.Count; i++)
            {
                var headerRow = csvData[i];
                var headerValue = new List<string>(headerRow.GetRange(1, headerRow.Count - 1));
                if (pdu.Headers == null)
                {
                    pdu.Headers = new Dictionary<string, List<string>>();
                }
                pdu.Headers.Add(headerRow[0], headerValue);
            }
            return pdu;
        }

        public byte[] Serialize()
        {
            var csvData = new List<List<string>>();
            csvData.Add(new List<string> { Version.ToString() });
            csvData.Add(new List<string> { PduType.ToString() });
            csvData.Add(new List<string> { Flags.ToString() });
            csvData.Add(new List<string> { Path ?? "" });
            csvData.Add(new List<string> { StatusIndicatesSuccess.ToString() });
            csvData.Add(new List<string> { StatusIndicatesClientError.ToString() });
            csvData.Add(new List<string> { StatusMessage ?? "" });
            csvData.Add(new List<string> { ContentLength.ToString() });
            csvData.Add(new List<string> { ContentType ?? "" });
            csvData.Add(new List<string> { Convert.ToBase64String(Data ?? new byte[0], DataOffset, DataLength) });
            foreach (var header in Headers ?? new Dictionary<string, List<string>>())
            {
                var headerRow = new List<string> { header.Key };
                headerRow.AddRange(header.Value);
                csvData.Add(headerRow);
            }
            var csv = CsvUtils.Serialize(csvData);
            var csvBytes = ByteUtils.StringToBytes(csv);
            if (!IncludeLengthPrefixDuringSerialization)
            {
                return csvBytes;
            }
            var pduBytes = new byte[4 + csvBytes.Length];
            Array.Copy(csvBytes, 0, pduBytes, 4, csvBytes.Length);
            ByteUtils.SerializeUpToInt64BigEndian(csvBytes.Length, pduBytes, 0, 4);
            return pduBytes;
        }
    }
}