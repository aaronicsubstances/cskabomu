using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Helper class for easier implementation of <see cref="IQuasiHttpBody"/> 
    /// by providing a default implementation. Provides writable
    /// content length and content type properties.
    /// Subclasses at a minimum are supposed to implement <see cref="ICustomReader"/>
    /// and <see cref="ICustomDisposable"/> interfaces.
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
        /// Gets or sets any string or null which can be used by the receiving end of the bytes generated
        /// by this instance, to determine how to interpret the bytes. It is equivalent to "Content-Type" header
        /// in HTTP.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Requires instance to implement <see cref="ICustomReader"/>, and
        /// copies from instance to supplied writer.
        /// </summary>
        /// <param name="writer">the writer which will be the destination of
        /// the bytes to be written.</param>
        /// <returns>a task representing asynchronous operation</returns>
        /// <exception cref="MissingDependencyException">
        /// if <see cref="ICustomReader"/> is not implemented</exception>
        public virtual Task WriteBytesTo(ICustomWriter writer)
        {
            var reader = this as ICustomReader;
            if (reader == null)
            {
                throw new MissingDependencyException(
                    "ICustomReader not implemented");
            }
            return IOUtils.CopyBytes(reader, writer);
        }

        public abstract Task CustomDispose();
    }
}
