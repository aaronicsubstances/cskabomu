using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class ConfigurableQuasiHttpServer : IQuasiHttpServer
    {
        public IQuasiHttpProcessingOptions DefaultProcessingOptions { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }
        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpServerTransport Transport { get; set; }
        public IMutexApi MutexApi { get; set; }
        public IMutexApiFactory MutexApiFactory { get; set; }
        public Func<Task> StartCallback { get; set; }
        public Func<Task> StopCallback { get; set; }
        public Func<IQuasiHttpRequest, IQuasiHttpProcessingOptions, Task<IQuasiHttpResponse>> SendToApplicationCallback { get; set; }

        public Task Start()
        {
            return StartCallback.Invoke();
        }

        public Task Stop()
        {
            return StopCallback.Invoke();
        }

        public Task<IQuasiHttpResponse> SendToApplication(IQuasiHttpRequest request,
            IQuasiHttpProcessingOptions options)
        {
            return SendToApplicationCallback.Invoke(request, options);
        }
    }
}
