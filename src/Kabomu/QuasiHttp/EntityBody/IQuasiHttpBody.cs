using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents a source of bytes which forms part of a quasi HTTP request or response.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface imposes a recommendation on its usage: values in Reader and Writable properties
    /// should only be used from 1 thread at a time; Hence implementations can take 
    /// advantage of this to optimize their code wherever possible.
    /// </para>
    /// </remarks>
    public interface IQuasiHttpBody : ICustomDisposable
    {
        /// <summary>
        /// Gets the number of bytes in the stream represented by this instance, or -1 (actually any negative value)
        /// to indicate an unknown number of bytes.
        /// </summary>
        long ContentLength { get; }

        /// <summary>
        /// Returns any string or null which can be used by the receiving end of the bytes generated
        /// by this instance, to determine how to interpret the bytes. It is equivalent to "Content-Type" header
        /// in HTTP.
        /// </summary>
        string ContentType { get; }

        ICustomReader Reader { get; }

        ICustomWritable Writable { get; }
    }
}
