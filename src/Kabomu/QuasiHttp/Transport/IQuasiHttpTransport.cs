using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Represents duplex stream of data from a real or virtual computer network. The expectations for implementations are that
    /// <list type="number">
    /// <item>writes, reads and connection releases must be implemented to allow for concurrent calls from different threads
    /// without thread-safety errors.
    /// </item>
    /// <item>connection releases should cause any ongoing write or read requests to fail</item>
    /// <item>multiple calls to connection releases must be tolerated.</item>
    /// <item>it is acceptable if an implementation chooses not to support concurrent multiple writes or concurrent multiple reads</item>
    /// </list>
    /// </summary>
    public interface IQuasiHttpTransport
    {
        Task WriteBytes(object connection, byte[] data, int offset, int length);
        Task<int> ReadBytes(object connection, byte[] data, int offset, int length);
        Task ReleaseConnection(object connection);
    }
}
