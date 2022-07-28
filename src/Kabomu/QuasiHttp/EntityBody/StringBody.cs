using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents byte stream derived from a string's UTF-8 representation.
    /// </summary>
    public class StringBody : SerializableObjectBody
    {
        /// <summary>
        /// Creates a new instance with the given string.
        /// </summary>
        /// <param name="content">string content</param>
        /// <exception cref="ArgumentNullException">if string argument is null</exception>
        public StringBody(string content) :
            base(content, SerializeContent)
        {
        }

        private static byte[] SerializeContent(object obj)
        {
            var content = (string)obj;
            var dataBytes = Encoding.UTF8.GetBytes(content);
            return dataBytes;
        }

        /// <summary>
        /// Gets the string data supplied at construction time which will be serialized.
        /// </summary>
        public string StringContent => (string)Content;
    }
}
