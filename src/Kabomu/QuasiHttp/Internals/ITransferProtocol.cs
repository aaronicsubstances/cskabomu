﻿using Kabomu.Common;
using System;

namespace Kabomu.QuasiHttp.Internals
{
    internal interface ITransferProtocol
    {
        IEventLoopApi EventLoop { get; }
        IQuasiHttpTransport Transport { get; }
    }
}