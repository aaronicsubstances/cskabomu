using System;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Implementation of <see cref="ISelfWritable"/> which delegates
    /// to externally supplied lambda functions.
    /// </summary>
    public class LambdaBasedCustomWritable : ISelfWritable
    {
        /// <summary>
        /// Gets or sets lambda function for performing write operation.
        /// </summary>
        public Func<object, Task> WritableFunc { get; set; }

        /// <summary>
        /// Calls upon <see cref="WritableFunc"/> to perform write operation.
        /// </summary>
        /// <param name="writer">the writer to pass to the <see cref="WritableFunc"/>
        /// lambda function</param>
        /// <returns>the task returned by calling the <see cref="WritableFunc"/>
        /// lambda function</returns>
        /// <exception cref="MissingDependencyException">If <see cref="WritableFunc"/>
        /// property is null</exception>
        public Task WriteBytesTo(object writer)
        {
            var writableFunc = WritableFunc;
            if (writableFunc == null)
            {
                throw new MissingDependencyException("WritableFunc");
            }
            return writableFunc.Invoke(writer);
        }
    }
}
