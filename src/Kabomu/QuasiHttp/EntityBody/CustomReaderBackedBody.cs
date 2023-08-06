using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents quasi http body based on an externally supplied reader.
    /// </summary>
    public class CustomReaderBackedBody : AbstractQuasiHttpBody
    {
        private readonly ICustomReader _backingReader;

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="wrappedReader">the reader which supplies bytes 
        /// during reading</param>
        /// <exception cref="ArgumentNullException">if reader argument is null</exception>
        public CustomReaderBackedBody(ICustomReader wrappedReader)
        {
            if (wrappedReader == null)
            {
                throw new ArgumentNullException(nameof(wrappedReader));
            }
            _backingReader = wrappedReader;
        }

        /// <summary>
        /// Returns backing reader supplied at construction time.
        /// </summary>
        public override ICustomReader Reader() => _backingReader;

        /// <summary>
        /// Disposes off backing reader supplied at construction time.
        /// </summary>
        public override Task CustomDispose() => _backingReader.CustomDispose();
    }
}
