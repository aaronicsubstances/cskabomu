using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    /// <summary>
    /// Structure used to encode quasi http headers for serialization and transmission on
    /// quasi http transports. All properties in this structure are optional except for Version.
    /// </summary>
    /// <remarks>
    /// This structure is equivalent to the information contained in
    /// HTTP request line, HTTP status line, and HTTP request and response headers.
    /// </remarks>
    public class LeadChunk
    {
        /// <summary>
        /// Gets or sets the serialization format version.
        /// </summary>
        public byte Version { get; set; }

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        public byte Flags { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of request target component of HTTP request line.
        /// </summary>
        public string RequestTarget { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of HTTP response status code.
        /// 
        /// NB: Must be valid signed 32-bit integer.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets a value providing the length in bytes of a quasi http body which will
        /// follow the lead chunk when serialized. Equivalent to Content-Length and 
        /// Transfer-Encoding=chunked HTTP headers.
        /// </summary>
        /// <remarks>
        /// There are three possible values:
        /// <list type="number">
        /// <item>zero: this means that there will be no quasi http body.</item>
        /// <item>positive: this means that there will be a quasi http body with the exact number of bytes
        /// present as the value of this property.</item>
        /// <item>negative: this means that there will be a quasi http body, but with an unknown number of
        /// bytes. This implies chunk encoding where one or more subsequent chunks will follow the
        /// lead chunk when serialized.
        /// </item>
        /// </list>
        /// 
        /// NB: Must be valid signed 48-bit integer.
        /// </remarks>
        public long ContentLength { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of method component of HTTP request line.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets an HTTP request or response version value.
        /// </summary>
        public string HttpVersion { get; set; }

        /// <summary>
        /// Gets or sets HTTP status text, ie the reason phrase component of HTTP response lines.
        /// </summary>
        public string HttpStatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of HTTP request or response headers. Null keys and values are not allowed.
        /// </summary>
        /// <remarks>
        /// Unlike in HTTP, here the headers are distinct from properties of this structure equivalent to 
        /// HTTP headers, i.e. Content-Length. So setting a Content-Length header
        /// here will have no bearing on how to transmit or receive quasi http bodies.
        /// </remarks>
        public IDictionary<string, IList<string>> Headers { get; set; }
    }
}
