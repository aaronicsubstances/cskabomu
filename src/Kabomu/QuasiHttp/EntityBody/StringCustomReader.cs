using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents byte stream derived from a string's UTF-8 representation.
    /// </summary>
    public class StringCustomReader : SerializableObjectCustomReader
    {
        /// <summary>
        /// Creates a new instance with the given string.
        /// </summary>
        /// <param name="content">string content</param>
        /// <exception cref="ArgumentNullException">if string argument is null</exception>
        public StringCustomReader(string content) :
            base(content, SerializeContent)
        {
        }

        private static byte[] SerializeContent(object obj)
        {
            var content = (string)obj;
            var dataBytes = Encoding.UTF8.GetBytes(content);
            return dataBytes;
        }
    }
}
