using System;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Implementation of <see cref="ICustomReader"/> and
    /// <see cref="ICustomWriter"/> interfaces which delegates
    /// to externally supplied lambda functions.
    /// </summary>
    public class LambdaBasedCustomReaderWriter : ICustomReader, ICustomWriter, ICustomDisposable
    {
        public Func<Task> Disposer { get; set; }

        /// <summary>
        /// Gets or sets lambda function for performing read operation.
        /// </summary>
        public Func<byte[], int, int, Task<int>> ReadFunc { get; set; }

        /// <summary>
        /// Calls upon <see cref="ReadFunc"/> to perform read operation.
        /// </summary>
        /// <param name="data">buffer to pass to <see cref="ReadFunc"/>
        /// lambda function for receiving read bytes</param>
        /// <param name="offset">starting position in buffer from which to store bytes read</param>
        /// <param name="length">number of bytes to read</param>
        /// <returns>the task returned by calling the <see cref="ReadFunc"/>
        /// lambda function</returns>
        /// <exception cref="MissingDependencyException">If <see cref="ReadFunc"/>
        /// property is null</exception>
        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            var readFunc = ReadFunc;
            if (readFunc == null)
            {
                throw new MissingDependencyException("ReadFunc");
            }
            return readFunc.Invoke(data, offset, length);
        }

        /// <summary>
        /// Gets or sets lambda function for performing write operation.
        /// </summary>
        public Func<byte[], int, int, Task> WriteFunc { get; set; }

        /// <summary>
        /// Calls upon <see cref="WriteFunc"/> to perform write operation.
        /// </summary>
        /// <param name="data">buffer to pass to <see cref="WriteFunc"/>
        /// lambda function for supplying bytes to write</param>
        /// <param name="offset">starting position in buffer from which
        /// to start transferring bytes</param>
        /// <param name="length">number of bytes to write</param>
        /// <returns>the task returned by calling the <see cref="WriteFunc"/>
        /// lambda function</returns>
        /// <exception cref="MissingDependencyException">If <see cref="WriteFunc"/>
        /// property is null</exception>
        public Task WriteBytes(byte[] data, int offset, int length)
        {
            var writeFunc = WriteFunc;
            if (writeFunc == null)
            {
                throw new MissingDependencyException("WriteFunc");
            }
            return writeFunc.Invoke(data, offset, length);
        }
    }
}
