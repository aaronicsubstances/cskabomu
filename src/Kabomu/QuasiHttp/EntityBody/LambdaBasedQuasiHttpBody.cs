using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Helper class providing a default implementation of <see cref="IQuasiHttpBody"/>,
    /// in which a custom reader is fetched from the <see cref="Reader"/> property, and
    /// copied over to the writer supplied by the <see cref="WriteBytesTo"/> method.
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

        /// <summary>
        /// Lambda function to which entire implementation of <see cref="WriteBytesTo"/> can
        /// be delegated to. Default implementation kicks in only
        /// if this property is null.
        /// </summary>
        public ISelfWritable SelfWritable { get; set; }

        /// <summary>
        /// Lambda function which can be used to provide a
        /// fallback readable stream, to support default
        /// implementation for <see cref="WriteBytesTo"/> method.
        /// </summary>
        public Func<object> ReaderFunc { get; set; }

        /// <summary>
        /// Gets or sets lambda function which can be used to release resources.
        /// </summary>
        public Func<Task> ReleaseFunc { get; set; }

        /// <summary>
        /// Invokes the <see cref="SelfWritable"/> property, and
        /// if that property is null, falls back to copying over
        /// value retrieved from <see cref="Reader"/> property to supplied writer.
        /// </summary>
        /// <param name="writer">the writer which will be the destination of
        /// the bytes to be written.</param>
        /// <returns>a task representing asynchronous operation</returns>
        /// <exception cref="MissingDependencyException">
        /// if <see cref="Reader"/> property returns null for use as fallback</exception>
        public virtual Task WriteBytesTo(object writer)
        {
            var writable = SelfWritable;
            if (writable != null)
            {
                return writable.WriteBytesTo(writer);
            }
            var reader = Reader;
            if (reader == null)
            {
                throw new MissingDependencyException(
                    "received null from ReaderFunc property");
            }
            return IOUtils.CopyBytes(reader, writer);
        }

        /// <summary>
        /// Returns value returned by invoking <see cref="ReaderFunc"/>.
        /// Returns null if <see cref="ReaderFunc"/> property is null.
        /// </summary>
        public object Reader => ReaderFunc?.Invoke();

        /// <summary>
        /// Invokes <see cref="ReleaseFunc"/>.
        /// Nothing is done if <see cref="ReleaseFunc"/> property is null.
        /// </summary>
        /// <returns>a task representing asynchronous operation</returns>
        public Task Release() => ReleaseFunc?.Invoke() ?? Task.CompletedTask;
    }
}
