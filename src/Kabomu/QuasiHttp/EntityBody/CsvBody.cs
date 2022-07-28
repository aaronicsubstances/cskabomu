using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents byte stream derived from a CSV serialized in UTF-8 encoding. This class was created
    /// to serve as a convenient means of representing actual HTTP forms (application/x-www-form-urlencoded),
    /// and query string portion of actual HTTP request lines/URLs. Both can be encoded as CSV rows, in which 
    /// <list type="number">
    /// <item>the first column of each row is a name or key</item>
    /// <item>the remaining columns are for the many possible values of the name or key</item>
    /// <item>each row can have a different number of columns</item>
    /// </list>
    /// </summary>
    public class CsvBody : SerializableObjectBody
    {
        /// <summary>
        /// Creates a new instance with the given CSV data.
        /// </summary>
        /// <param name="content">CSV data.</param>
        /// <exception cref="ArgumentNullException">if CSV data argument is null</exception>
        public CsvBody(Dictionary<string, List<string>> content):
            base(content, SerializeContent)
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

        /// <summary>
        /// Gets the CSV data supplied at construction time which will be serialized.
        /// </summary>
        public Dictionary<string, List<string>> CsvContent => (Dictionary<string, List<string>>)Content;
    }
}
