using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents quasi http body based on a string in UTF8 encoding.
    /// </summary>
    public class StringBody : AbstractQuasiHttpBody, ICustomReader
    {
        private ByteBufferBody _backingBody;

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

        public override Task CustomDispose() => Task.CompletedTask;

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            if (_backingBody == null)
            {
                _backingBody = new ByteBufferBody(ByteUtils.StringToBytes(Content));
            }
            return _backingBody.ReadBytes(data, offset, length);
        }

        public override Task WriteBytesTo(ICustomWriter writer)
        {
            if (_backingBody == null)
            {
                _backingBody = new ByteBufferBody(ByteUtils.StringToBytes(Content));
            }
            return _backingBody.WriteBytesTo(writer);
        }
    }
}
