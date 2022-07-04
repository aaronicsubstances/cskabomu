using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpServer
    {
        int MaxChunkSize { get; set; }
        UncaughtErrorCallback ErrorHandler { get; set; }
        int OverallReqRespTimeoutMillis { get; set; }
        IQuasiHttpApplication Application { get; set; }
        IQuasiHttpServerTransport Transport { get; set; }
        Task Start();
        Task Stop();
        IMutexApi MutexApi { get; set; }
        IMutexApiFactory MutexApiFactory { get; set; }
    }
}
