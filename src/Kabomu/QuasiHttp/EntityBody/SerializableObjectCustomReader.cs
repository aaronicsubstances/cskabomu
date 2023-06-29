using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
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

        public Task<int> ReadAsync(byte[] data, int offset, int length)
        {
            EnsureSerialization();
            return _backingBody.ReadAsync(data, offset, length);
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

        public Task CloseAsync()
        {
            return _backingBody?.CloseAsync() ?? Task.CompletedTask;
        }

        public Task WriteToAsync(ICustomWriter writer, IDictionary<string, object> context)
        {
            EnsureSerialization();
            return _backingBody.WriteToAsync(writer, context);
        }
    }
}
