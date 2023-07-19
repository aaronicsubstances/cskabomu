using System;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Implementation of <see cref="ICustomWritable"/> which delegates
    /// to externally supplied lambda functions.
    /// </summary>
    public class LambdaBasedCustomWritable : ICustomWritable
    {
        /// <summary>
        /// Gets or sets lambda function for performing write operation.
        /// </summary>
        public Func<ICustomWriter, Task> WritableFunc { get; set; }

        /// <summary>
        /// Gets or sets lambda function for performing dispose operation.
        /// </summary>
        public Func<Task> DisposeFunc { get; set; }

        /// <summary>
        /// Calls upon <see cref="WritableFunc"/> to perform write operation.
        /// </summary>
        /// <param name="writer">the writer to pass to the <see cref="WritableFunc"/>
        /// lambda function</param>
        /// <returns>the task returned by calling the <see cref="WritableFunc"/>
        /// lambda function</returns>
        /// <exception cref="MissingDependencyException">If <see cref="WritableFunc"/>
        /// property is null</exception>
        public Task WriteBytesTo(ICustomWriter writer)
        {
            var writableFunc = WritableFunc;
            if (writableFunc == null)
            {
                throw new MissingDependencyException("WritableFunc");
            }
            return writableFunc.Invoke(writer);
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
