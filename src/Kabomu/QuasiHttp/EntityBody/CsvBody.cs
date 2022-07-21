using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class CsvBody : IQuasiHttpBody
    {
        private readonly SerializableObjectBody _backingBody;

        public CsvBody(Dictionary<string, List<string>> content)
        {
            _backingBody = new SerializableObjectBody(content,
                SerializeContent, TransportUtils.ContentTypeCsv);
        }

        private static byte[] SerializeContent(object obj)
        {
            var content = (Dictionary<string, List<string>>)obj;
            var rows = new List<List<string>>();
            foreach (var entry in content)
            {
                var row = new List<string>();
                row.Add(entry.Key);
                row.AddRange(entry.Value);
                rows.Add(row);
            }
            var csv = CsvUtils.Serialize(rows);
            var csvBytes = Encoding.UTF8.GetBytes(csv);
            return csvBytes;
        }

        public Dictionary<string, List<string>> Content => (Dictionary<string, List<string>>)_backingBody.Content;
        public long ContentLength => _backingBody.ContentLength;
        public string ContentType => _backingBody.ContentType;


        public Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            return _backingBody.ReadBytes(data, offset, bytesToRead);
        }

        public Task EndRead()
        {
            return _backingBody.EndRead();
        }
    }
}
