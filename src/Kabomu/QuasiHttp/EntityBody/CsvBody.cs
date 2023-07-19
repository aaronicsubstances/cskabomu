using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents quasi http body based CSV serialized in UTF8 encoding. This class was created
    /// to serve as a convenient means of representing actual HTTP forms (application/x-www-form-urlencoded),
    /// and query string portion of actual HTTP request lines/URLs. Both can be encoded as CSV rows, in which 
    /// <list type="number">
    /// <item>the first column of each row is a name or key</item>
    /// <item>the remaining columns are for the many possible values of the name or key</item>
    /// <item>each row can have a different number of columns</item>
    /// </list>
    /// </summary>
    public class CsvBody : AbstractQuasiHttpBody, ICustomReader
    {
        private StringBody _backingBody;

        /// <summary>
        /// Creates a new instance with the given CSV content.
        /// </summary>
        /// <param name="content">CSV content</param>
        /// <exception cref="ArgumentNullException">if content argument is null</exception>
        public CsvBody(IDictionary<string, IList<string>> content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            Content = content;
        }

        /// <summary>
        /// Returns the CSV rows serving as the source of bytes for the instance.
        /// </summary>
        public IDictionary<string, IList<string>> Content { get; }

        public override Task CustomDispose() => Task.CompletedTask;

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            if (_backingBody == null)
            {
                _backingBody = new StringBody(SerializeContent());
            }
            return _backingBody.ReadBytes(data, offset, length);
        }

        public override Task WriteBytesTo(ICustomWriter writer)
        {
            if (_backingBody == null)
            {
                _backingBody = new StringBody(SerializeContent());
            }
            return _backingBody.WriteBytesTo(writer);
        }

        private string SerializeContent()
        {
            var rows = new List<IList<string>>();
            foreach (var entry in Content)
            {
                var row = new List<string>();
                row.Add(entry.Key);
                row.AddRange(entry.Value);
                rows.Add(row);
            }
            var csv = CsvUtils.Serialize(rows);
            return csv;
        }
    }
}
