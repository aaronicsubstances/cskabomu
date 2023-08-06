using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents quasi http body based on a string in UTF8 encoding.
    /// </summary>
    public class StringBody : AbstractQuasiHttpBody
    {
        /// <summary>
        /// Creates a new instance with the given string. The content length is
        /// initialized to the byte count of the string in UTF8 encoding.
        /// </summary>
        /// <param name="content">string content</param>
        /// <exception cref="ArgumentNullException">if string argument is null</exception>
        public StringBody(string content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            Content = content;
            ContentLength = Encoding.UTF8.GetByteCount(content);
        }

        /// <summary>
        /// Returns the string serving as the source of bytes for the instance.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public override Task CustomDispose() => Task.CompletedTask;

        /// <summary>
        /// Returns a freshly created reader backed by
        /// <see cref="Content"/> property in UTF8 encoding.
        /// </summary>
        public override ICustomReader Reader()
        {
            var stream = new MemoryStream(ByteUtils.StringToBytes(
                Content));
            return new StreamCustomReaderWriter(stream);
        }

        /// <summary>
        /// Transfers contents of <see cref="Content"/> property
        /// to supplied writer in UTF8 encoding.
        /// </summary>
        /// <param name="writer">supplied writer</param>
        public override Task WriteBytesTo(ICustomWriter writer)
        {
            var backingBody = new ByteBufferBody(ByteUtils.StringToBytes(Content));
            return backingBody.WriteBytesTo(writer);
        }
    }
}
