using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents quasi http body based CSV serialized in UTF-8 encoding. This class was created
    /// to serve as a convenient means of representing actual HTTP forms (application/x-www-form-urlencoded),
    /// and query string portion of actual HTTP request lines/URLs. Both can be encoded as CSV rows, in which 
    /// <list type="number">
    /// <item>the first column of each row is a name or key</item>
    /// <item>the remaining columns are for the many possible values of the name or key</item>
    /// <item>each row can have a different number of columns</item>
    /// </list>
    /// </summary>
    public class CsvBody : IQuasiHttpBody
    {
        /// <summary>
        /// Creates a new instance with the given CSV content.
        /// Initializes content length to -1.
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

        public long ContentLength { get; set; } = -1;

        /// <summary>
        /// Returns the CSV rows serving as the source of bytes for the instance.
        /// </summary>
        public IDictionary<string, IList<string>> Content { get; }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public Task Release() => Task.CompletedTask;

        /// <summary>
        /// Returns a freshly created <see cref="Stream"/> instance backed by
        /// <see cref="Content"/> property in UTF-8 encoding.
        /// </summary>
        public object Reader
        {
            get
            {
                return new MemoryStream(ByteUtils.StringToBytes(
                    SerializeContent()));
            }
        }

        /// <summary>
        /// Transfers contents of <see cref="Content"/> property
        /// to supplied writer in UTF-8 encoding.
        /// </summary>
        /// <param name="writer">supplied writer</param>
        public Task WriteBytesTo(object writer)
        {
            return IOUtils.CopyBytes(Reader, writer);
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
