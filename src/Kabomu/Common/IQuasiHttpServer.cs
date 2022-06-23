using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public interface IQuasiHttpServer
    {
        int MaxChunkSize { get; set; }
        UncaughtErrorCallback ErrorHandler { get; set; }
        int OverallReqRespTimeoutMillis { get; set; }
        IQuasiHttpApplication Application { get; set; }
        IQuasiHttpTransport Transport { get; set; }
        Task Start();
        Task Stop();
    }
}
