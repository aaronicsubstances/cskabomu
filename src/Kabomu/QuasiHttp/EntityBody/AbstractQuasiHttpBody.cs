using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Helper class for easier implementation of <see cref="IQuasiHttpBody"/>.
    /// Provides a default implementation of the WriteBytesTo method
    /// based on a non-null value returned by Reader() method. Also provides writable
    /// content length property.
    /// </summary>
    public abstract class AbstractQuasiHttpBody : IQuasiHttpBody
    {
        /// <summary>
        /// Creates a new instance and initializes <see cref="ContentLength"/>
        /// property to -1.
        /// </summary>
        public AbstractQuasiHttpBody()
        {
            
        }

        /// <summary>
        /// Gets or sets the number of bytes in the stream represented by this instance,
        /// or -1 (actually any negative value) to indicate an unknown number of bytes.
        /// </summary>
        public long ContentLength { get; set; } = -1;

        /// <summary>
        /// Copies bytes from a value retrieved from Reader() method to supplied writer.
        /// </summary>
        /// <param name="writer">the writer which will be the destination of
        /// the bytes to be written.</param>
        /// <returns>a task representing asynchronous operation</returns>
        /// <exception cref="MissingDependencyException">
        /// if <see cref="Reader"/> method returns null</exception>
        public virtual Task WriteBytesTo(ICustomWriter writer)
        {
            var reader = Reader();
            if (reader == null)
            {
                throw new MissingDependencyException(
                    "received null from Reader() method");
            }
            return IOUtils.CopyBytes(reader, writer);
        }

        public abstract ICustomReader Reader();

        public abstract Task CustomDispose();
    }
}
