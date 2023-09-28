using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Represents commonality of functions provided by TCP or IPC mechanisms
    /// at both server and client ends.
    /// </summary>
    public interface IQuasiHttpTransport
    {
        Stream GetReadableStream(IQuasiHttpConnection connection);
        Stream GetWritableStream(IQuasiHttpConnection connection);
    }
}
