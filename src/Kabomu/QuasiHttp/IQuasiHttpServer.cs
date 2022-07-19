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
        IQuasiHttpProcessingOptions DefaultProcessingOptions { get; set; }
        UncaughtErrorCallback ErrorHandler { get; set; }
        IQuasiHttpApplication Application { get; set; }
        IQuasiHttpServerTransport Transport { get; set; }
        IMutexApi MutexApi { get; set; }
        IMutexApiFactory MutexApiFactory { get; set; }
        Task Start();
        Task Stop();
        Task<IQuasiHttpResponse> SendToApplication(IQuasiHttpRequest request,
            IQuasiHttpProcessingOptions options);
    }
}
