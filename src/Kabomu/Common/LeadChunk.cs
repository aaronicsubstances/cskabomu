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
            var specialHeaderRow = new List<string>();
            specialHeaderRow.Add((Path != null).ToString());
            specialHeaderRow.Add(Path ?? "");
            specialHeaderRow.Add(StatusIndicatesSuccess.ToString());
            specialHeaderRow.Add(StatusIndicatesClientError.ToString());
            specialHeaderRow.Add((StatusMessage != null).ToString());
            specialHeaderRow.Add(StatusMessage ?? "");
            specialHeaderRow.Add(HasContent.ToString());
            specialHeaderRow.Add((ContentType != null).ToString());
            specialHeaderRow.Add(ContentType ?? "");
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
            instance.Flags = data[offset + 1];

            var csv = ByteUtils.BytesToString(data, offset + 2, length - 2);
            var csvData = CsvUtils.Deserialize(csv);
            if (csvData.Count == 0)
            {
                throw new ArgumentException("invalid lead chunk");
            }
            var specialHeader = csvData[0];
            if (specialHeader.Count < 9)
            {
                throw new ArgumentException("invalid special header");
            }
            if (bool.Parse(specialHeader[0]))
            {
                instance.Path = specialHeader[1];
            }
            instance.StatusIndicatesSuccess = bool.Parse(specialHeader[2]);
            instance.StatusIndicatesClientError = bool.Parse(specialHeader[3]);
            if (bool.Parse(specialHeader[4]))
            {
                instance.StatusMessage = specialHeader[5];
            }
            instance.HasContent = bool.Parse(specialHeader[6]);
            if (bool.Parse(specialHeader[7]))
            {
                instance.ContentType = specialHeader[8];
            }
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
