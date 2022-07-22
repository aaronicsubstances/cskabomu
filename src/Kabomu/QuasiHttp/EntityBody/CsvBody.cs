using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class CsvBody : SerializableObjectBody
    {
        public CsvBody(Dictionary<string, List<string>> content, string contentType):
            base(content, SerializeContent, contentType)
        {
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

        public Dictionary<string, List<string>> CsvContent => (Dictionary<string, List<string>>)Content;
    }
}
