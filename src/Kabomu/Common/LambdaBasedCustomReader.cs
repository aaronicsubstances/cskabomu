using System;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Implementation of <see cref="ICustomReader"/> which delegates
    /// to externally supplied lambda functions.
    /// </summary>
    public class LambdaBasedCustomReader : ICustomReader
    {
        /// <summary>
        /// Gets or sets lambda function for performing read operation.
        /// </summary>
        public Func<byte[], int, int, Task<int>> ReadFunc { get; set; }
        
        /// <summary>
        /// Gets or sets lambda function for performing dispose operation.
        /// </summary>
        public Func<Task> DisposeFunc { get; set; }

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
