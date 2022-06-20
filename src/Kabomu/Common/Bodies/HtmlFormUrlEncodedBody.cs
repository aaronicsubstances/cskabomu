using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common.Bodies
{
    public class HtmlFormUrlEncodedBody : IQuasiHttpBody
    {
        private readonly SerializableObjectBody _backingBody;

        public HtmlFormUrlEncodedBody(Dictionary<string, List<string>> content)
        {
            _backingBody = new SerializableObjectBody(content,
                SerializeContent, TransportUtils.ContentTypeHtmlFormUrlEncoded);
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

        public Task<int> ReadBytes(IEventLoopApi eventLoop, byte[] data, int offset, int bytesToRead)
        {
            return _backingBody.ReadBytes(eventLoop, data, offset, bytesToRead);
        }

        public Task EndRead(IEventLoopApi eventLoop, Exception e)
        {
            return _backingBody.EndRead(eventLoop, e);
        }
    }
}
