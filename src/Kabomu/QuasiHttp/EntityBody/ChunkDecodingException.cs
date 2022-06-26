using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class ChunkDecodingException : Exception
    {
        public ChunkDecodingException()
        {
        }

        public ChunkDecodingException(string message) : base(message)
        {
        }

        public ChunkDecodingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ChunkDecodingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
