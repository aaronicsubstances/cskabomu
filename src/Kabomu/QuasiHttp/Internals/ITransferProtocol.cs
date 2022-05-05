using Kabomu.Common;
using System;

namespace Kabomu.QuasiHttp.Internals
{
    internal interface ITransferProtocol
    {
        IQuasiHttpTransport Transport { get; }
        IEventLoopApi EventLoop { get; }
    }
}