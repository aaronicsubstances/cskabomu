using System;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Implementation of <see cref="ICustomWriter"/> which delegates
    /// to externally supplied lambda functions.
    /// </summary>
    public class LambdaBasedCustomWriter : ICustomWriter
    {
        /// <summary>
        /// Gets or sets lambda function for performing write operation.
        /// </summary>
        public Func<byte[], int, int, Task> WriteFunc { get; set; }

        /// <summary>
        /// Gets or sets lambda function for performing dispose operation.
        /// </summary>
        public Func<Task> DisposeFunc { get; set; }

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

        /// <summary>
        /// Calls upon <see cref="DisposeFunc"/> to perform dispose operation.
        /// Nothing is done if <see cref="DisposeFunc"/> property is null.
        /// </summary>
        /// <returns>a task representing asynchronous operation</returns>
        public Task CustomDispose()
        {
            return DisposeFunc?.Invoke() ?? Task.CompletedTask;
        }
    }
}
