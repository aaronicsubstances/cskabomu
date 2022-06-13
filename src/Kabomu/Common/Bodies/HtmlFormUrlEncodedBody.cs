using System;
using System.Collections.Generic;
using System.Text;

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

        public void ReadBytes(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            _backingBody.ReadBytes(mutex, data, offset, bytesToRead, cb);
        }

        public void OnEndRead(IMutexApi mutex, Exception e)
        {
            _backingBody.OnEndRead(mutex, e);
        }
    }
}
