using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
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
        public bool HasContent { get; set; }
        public string ContentType { get; set; }
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
            csvData.Add(Path != null ? new List<string> { Path } : new List<string>());
            csvData.Add(new List<string> { StatusIndicatesSuccess.ToString() });
            csvData.Add(new List<string> { StatusIndicatesClientError.ToString() });
            csvData.Add(StatusMessage != null ? new List<string> { StatusMessage } : new List<string>());
            csvData.Add(new List<string> { HasContent.ToString() });
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
            instance.Flags = data[offset + 1];

            var csv = ByteUtils.BytesToString(data, offset + 2, length - 2);
            var csvData = CsvUtils.Deserialize(csv);
            if (csvData.Count < 6)
            {
                throw new ArgumentException("invalid lead chunk");
            }
            if (csvData[0].Count > 0)
            {
                instance.Path = csvData[0][0];
            }
            instance.StatusIndicatesSuccess = bool.Parse(csvData[1][0]);
            instance.StatusIndicatesClientError = bool.Parse(csvData[2][0]);
            if (csvData[3].Count > 0)
            {
                instance.StatusMessage = csvData[3][0];
            }
            instance.HasContent = bool.Parse(csvData[4][0]);
            if (csvData[5].Count > 0)
            {
                instance.ContentType = csvData[5][0];
            }
            for (int i = 6; i < csvData.Count; i++)
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
