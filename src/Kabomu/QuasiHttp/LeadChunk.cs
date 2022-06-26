using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class LeadChunk
    {
        public const byte Version01 = 1;

        public byte Version { get; set; }
        public byte Flags { get; set; }
        public string Path { get; set; }
        public bool StatusIndicatesSuccess { get; set; }
        public bool StatusIndicatesClientError { get; set; }
        public string StatusMessage { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
        public string HttpMethod { get; set; }
        public string HttpVersion { get; set; }
        public int HttpStatusCode { get; set; }
        public Dictionary<string, List<string>> Headers { get; set; }

        public ByteBufferSlice[] Serialize()
        {
            var serialized = new ByteBufferSlice[2];

            var csvDataPrefix = new byte[] { Version, Flags };
            serialized[0] = new ByteBufferSlice
            {
                Data = csvDataPrefix,
                Length = csvDataPrefix.Length
            };

            var csvData = new List<List<string>>();
            var specialHeaderRow = new List<string>();
            specialHeaderRow.Add((Path != null ? 1 : 0).ToString());
            specialHeaderRow.Add(Path ?? "");
            specialHeaderRow.Add((StatusIndicatesSuccess ? 1 : 0).ToString());
            specialHeaderRow.Add((StatusIndicatesClientError ? 1 : 0).ToString());
            specialHeaderRow.Add((StatusMessage != null ? 1 : 0).ToString());
            specialHeaderRow.Add(StatusMessage ?? "");
            specialHeaderRow.Add(ContentLength.ToString());
            specialHeaderRow.Add((ContentType != null ? 1 : 0).ToString());
            specialHeaderRow.Add(ContentType ?? "");
            specialHeaderRow.Add((HttpMethod != null ? 1 : 0).ToString());
            specialHeaderRow.Add(HttpMethod ?? "");
            specialHeaderRow.Add((HttpVersion != null ? 1 : 0).ToString());
            specialHeaderRow.Add(HttpVersion ?? "");
            specialHeaderRow.Add(HttpStatusCode.ToString());
            csvData.Add(specialHeaderRow);
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
            serialized[1] = new ByteBufferSlice
            {
                Data = csvBytes,
                Length = csvBytes.Length
            };
            return serialized;
        }

        public static LeadChunk Deserialize(byte[] data, int offset, int length)
        {
            if (length < 10)
            {
                throw new ArgumentException("too small to be a valid lead chunk");
            }

            var instance = new LeadChunk();
            instance.Version = data[offset];
            if (instance.Version == 0)
            {
                throw new ArgumentException("version not set");
            }
            instance.Flags = data[offset + 1];

            var csv = ByteUtils.BytesToString(data, offset + 2, length - 2);
            var csvData = CsvUtils.Deserialize(csv);
            if (csvData.Count == 0)
            {
                throw new ArgumentException("invalid lead chunk");
            }
            var specialHeader = csvData[0];
            if (specialHeader.Count < 14)
            {
                throw new ArgumentException("invalid special header");
            }
            if (specialHeader[0] != "0")
            {
                instance.Path = specialHeader[1];
            }
            instance.StatusIndicatesSuccess = specialHeader[2] != "0";
            instance.StatusIndicatesClientError = specialHeader[3] != "0";
            if (specialHeader[4] != "0")
            {
                instance.StatusMessage = specialHeader[5];
            }
            instance.ContentLength = long.Parse(specialHeader[6]);
            if (specialHeader[7] != "0")
            {
                instance.ContentType = specialHeader[8];
            }
            if (specialHeader[9] != "0")
            {
                instance.HttpMethod = specialHeader[10];
            }
            if (specialHeader[11] != "0")
            {
                instance.HttpVersion = specialHeader[12];
            }
            instance.HttpStatusCode = int.Parse(specialHeader[13]);
            for (int i = 1; i < csvData.Count; i++)
            {
                var headerRow = csvData[i];
                if (headerRow.Count < 2)
                {
                    continue;
                }
                var headerValue = new List<string>(headerRow.GetRange(1, headerRow.Count - 1));
                if (instance.Headers == null)
                {
                    instance.Headers = new Dictionary<string, List<string>>();
                }
                instance.Headers.Add(headerRow[0], headerValue);
            }

            return instance;
        }
    }
}
