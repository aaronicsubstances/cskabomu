using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Helper class for easier implementation of <see cref="IQuasiHttpBody"/>.
    /// </summary>
    public class LambdaBasedQuasiHttpBody : IQuasiHttpBody
    {
        /// <summary>
        /// Creates a new instance and initializes <see cref="ContentLength"/>
        /// property to -1.
        /// </summary>
        public LambdaBasedQuasiHttpBody()
        {
            
        }

        /// <summary>
        /// Gets or sets the number of bytes that this instance will supply,
        /// or -1 (actually any negative value) to indicate an unknown number of bytes.
        /// </summary>
        public long ContentLength { get; set; } = -1;

        public Func<ICustomWritable> WritableFunc { get; set; }

        public Func<object> ReaderFunc { get; set; }

        /// <summary>
        /// Gets or sets lambda function for releasing resources.
        /// </summary>
        public Func<Task> ReleaseFunc { get; set; }

        /// <summary>
        /// Invokes value retried from <see cref="WritableFunc"/> property, and
        /// if that value is null, falls back to copying over
        /// value retrieved from <see cref="ReaderFunc"/> property to supplied writer.
        /// </summary>
        /// <param name="writer">the writer which will be the destination of
        /// the bytes to be written.</param>
        /// <returns>a task representing asynchronous operation</returns>
        /// <exception cref="MissingDependencyException">
        /// if <see cref="Reader"/> property returns null</exception>
        public virtual Task WriteBytesTo(object writer)
        {
            var writable = WritableFunc?.Invoke();
            if (writable != null)
            {
                return writable.WriteBytesTo(writer);
            }
            var reader = Reader;
            if (reader == null)
            {
                throw new MissingDependencyException(
                    "received null from Reader property");
            }
            return IOUtils.CopyBytes(reader, writer);
        }

        public object Reader => ReaderFunc?.Invoke();

        /// <summary>
        /// Calls upon <see cref="ReleaseFunc"/> to perform dispose operation.
        /// Nothing is done if <see cref="ReleaseFunc"/> property is null.
        /// </summary>
        /// <returns>a task representing asynchronous operation</returns>
        public Task Release() => ReleaseFunc?.Invoke() ?? Task.CompletedTask;
    }
}
