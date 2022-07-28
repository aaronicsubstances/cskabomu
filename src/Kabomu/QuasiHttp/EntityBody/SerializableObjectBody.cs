using Kabomu.Common;
using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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
    public class SerializableObjectBody : IQuasiHttpBody
    {
        private readonly ICancellationHandle _readCancellationHandle = new DefaultCancellationHandle();
        private IQuasiHttpBody _backingBody;

        /// <summary>
        /// Creates a new instance with any object which can be converted into bytes through serialization.
        /// </summary>
        /// <param name="content">object which will be converted into bytes via serialization.</param>
        /// <param name="serializationHandler">Initial serialization function. Can be null, in which
        /// case it must be set via corresponding property for initial read to succeed.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="content"/> argument is null.</exception>
        public SerializableObjectBody(object content, Func<object, byte[]> serializationHandler)
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

        /// <summary>
        /// Returns -1 to indicate unknown length.
        /// </summary>
        public long ContentLength => -1;

        public string ContentType { get; set; }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            if (_backingBody == null)
            {
                var serializationHandler = SerializationHandler;
                if (serializationHandler == null)
                {
                    throw new MissingDependencyException("serialization handler");
                }
                var srcData = serializationHandler.Invoke(Content);
                _backingBody = new ByteBufferBody(srcData, 0, srcData.Length);
            }
            int bytesRead = await _backingBody.ReadBytes(data, offset, bytesToRead);
            return bytesRead;
        }

        public Task EndRead()
        {
            _readCancellationHandle.Cancel();
            // don't bother about ending read of backing body since it is just an in-memory object
            // and there is no contract to cancel ongoing reads.
            // that spares us from dealing with possible null reference and memory inconsistency
            // in determining whether backing body has been initialized or not.
            return Task.CompletedTask;
        }
    }
}
