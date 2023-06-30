using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents byte stream derived from a string's UTF-8 representation.
    /// </summary>
    public class StringBody : AbstractQuasiHttpBody, ICustomReader
    {
        private ByteBufferBody _backingBody;

        /// <summary>
        /// Creates a new instance with the given string.
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

        public string Content { get; }

        public override Task CustomDispose() => Task.CompletedTask;

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            if (_backingBody == null)
            {
                _backingBody = new ByteBufferBody(Encoding.UTF8.GetBytes(Content));
            }
            return _backingBody.ReadBytes(data, offset, length);
        }

        public override Task WriteBytesTo(ICustomWriter writer)
        {
            if (_backingBody == null)
            {
                _backingBody = new ByteBufferBody(Encoding.UTF8.GetBytes(Content));
            }
            return _backingBody.WriteBytesTo(writer);
        }
    }
}
