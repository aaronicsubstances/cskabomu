using Kabomu.Common;
using System;

namespace Kabomu.QuasiHttp.Internals
{
    internal interface ITransferProtocol
    {
        IEventLoopApi EventLoop { get; }
        void SendPdu(QuasiHttpPdu pdu, object connectionHandle, Action<Exception> cb);
    }
}