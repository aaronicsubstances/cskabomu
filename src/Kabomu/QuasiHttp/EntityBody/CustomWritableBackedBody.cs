using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents a quasi http body based on an externally supplied
    /// writable instance.
    /// </summary>
    public class CustomWritableBackedBody : AbstractQuasiHttpBody
    {
        private readonly ICustomWritable _writable;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="writable">writable to which writes will
        /// be delegated.</param>
        /// <exception cref="ArgumentNullException">if writable argument
        /// is null</exception>
        public CustomWritableBackedBody(ICustomWritable writable)
        {
            if (writable == null)
            {
                throw new ArgumentNullException(nameof(writable));
            }
            _writable = writable;
        }

        /// <summary>
        /// Returns null since this class does not support direct reading.
        /// </summary>
        public override ICustomReader Reader() => null;

        /// <summary>
        /// Disposes of backing writable supplied at construction time.
        /// </summary>
        public override Task CustomDispose() => _writable.CustomDispose();

        /// <summary>
        /// Passes supplied writer to backing writable supplied at
        /// construction time.
        /// </summary>
        /// <param name="writer">supplied writer</param>
        public override Task WriteBytesTo(ICustomWriter writer) =>
            _writable.WriteBytesTo(writer);
    }
}
