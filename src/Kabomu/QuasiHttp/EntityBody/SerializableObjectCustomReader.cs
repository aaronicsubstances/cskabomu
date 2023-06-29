using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents a byte stream which is derived from lazy serialization an in-memory object, ie 
    /// serialization isn't done until ReadBytes() is called.
    /// </summary>
    /// <remarks>
    /// This class is implemented with the interest of memory-based transport in mind, and that is the
    /// reason why serialization handler is not required at construction time, or why serialization is
    /// not done eagerly at construction time. This then makes it possible for memory-based communications
    /// to avoid performance hits due to serialization.
    /// </remarks>
    public class SerializableObjectCustomReader : ICustomReader, ICustomWritable<IDictionary<string, object>>
    {
        private ByteBufferCustomReader _backingBody;

        /// <summary>
        /// Creates a new instance with any object which can be converted into bytes through serialization.
        /// </summary>
        /// <param name="content">object which will be converted into bytes via serialization.</param>
        /// <param name="serializationHandler">Initial serialization function. Can be null, in which
        /// case it must be set via corresponding property for initial read to succeed.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="content"/> argument is null.</exception>
        public SerializableObjectCustomReader(object content, Func<object, byte[]> serializationHandler)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            Content = content;
            SerializationHandler = serializationHandler;
        }

        /// <summary>
        /// Gets or sets the function which will be responsible for serializing the content property
        /// to supply byte read requests.
        /// </summary>
        public Func<object, byte[]> SerializationHandler { get; set; }

        /// <summary>
        /// Gets the object which will serve as the source of bytes for read requests. Same as the object
        /// supplied at construction time.
        /// </summary>
        public object Content { get; }

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            EnsureSerialization();
            return _backingBody.ReadBytes(data, offset, length);
        }

        private void EnsureSerialization()
        {
            if (_backingBody == null)
            {
                var serializationHandler = SerializationHandler;
                if (serializationHandler == null)
                {
                    throw new MissingDependencyException("serialization handler");
                }
                var srcData = serializationHandler.Invoke(Content);
                _backingBody = new ByteBufferCustomReader(srcData);
            }
        }

        public Task CustomDispose()
        {
            return _backingBody?.CustomDispose() ?? Task.CompletedTask;
        }

        public Task WriteBytesTo(ICustomWriter writer, IDictionary<string, object> context)
        {
            EnsureSerialization();
            return _backingBody.WriteBytesTo(writer, context);
        }
    }
}
