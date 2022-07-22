using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    public interface IQuasiHttpServer
    {
        IQuasiHttpProcessingOptions DefaultProcessingOptions { get; set; }
        UncaughtErrorCallback ErrorHandler { get; set; }
        IQuasiHttpApplication Application { get; set; }
        IQuasiHttpServerTransport Transport { get; set; }
        IMutexApi MutexApi { get; set; }
        Task Start();
        Task Stop(int resetTimeMillis);
        Task Reset(Exception cause);
        Task<IQuasiHttpResponse> SendToApplication(IQuasiHttpRequest request,
            IQuasiHttpProcessingOptions options);
    }
}
