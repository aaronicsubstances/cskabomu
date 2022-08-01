using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Server;
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
        public ITimerApi TimerApi { get; set; }
        public Func<Task> StartCallback { get; set; }
        public Func<int, Task> StopCallback { get; set; }
        public Func<Exception, Task> ResetCallback { get; set; }
        public Func<IQuasiHttpRequest, IQuasiHttpProcessingOptions, Task<IQuasiHttpResponse>> SendToApplicationCallback { get; set; }

        public Task Start()
        {
            return StartCallback.Invoke();
        }

        public Task Stop(int resetTimeMillis)
        {
            return StopCallback.Invoke(resetTimeMillis);
        }

        public Task Reset(Exception cause)
        {
            return ResetCallback.Invoke(cause);
        }

        public Task<IQuasiHttpResponse> ProcessReceiveRequest(IQuasiHttpRequest request,
            IQuasiHttpProcessingOptions options)
        {
            return SendToApplicationCallback.Invoke(request, options);
        }
    }
}
