using Kabomu.Concurrency;
using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Registry;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator
{
    public class MediatorQuasiWebApplication : IQuasiHttpApplication
    {
        public IList<Handler> InitialHandlers { get; set; }
        public IRegistry InitialHandlerVariables { get; set; }
        public IRegistry HandlerConstants { get; set; }
        public IMutexApiFactory MutexApiFactory { get; set; }

        public async Task<IQuasiHttpResponse> ProcessRequest(IQuasiHttpRequest request, IDictionary<string, object> requestEnvironment)
        {
            var contextRequest = new DefaultContextRequest(request, requestEnvironment);
            var responseTransmmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var contextResponse = new DefaultContextResponse(new DefaultQuasiHttpResponse(), responseTransmmitter);
            var context = new DefaultContext
            {
                Request = contextRequest,
                Response = contextResponse,
                InitialHandlers = InitialHandlers,
                InitialHandlerVariables = InitialHandlerVariables,
                HandlerConstants = HandlerConstants,
            };

            var mutexApiTask = MutexApiFactory?.Create();
            if (mutexApiTask != null)
            {
                context.MutexApi = await mutexApiTask;
            }

            await context.Start();

            return await responseTransmmitter.Task;
        }
    }
}
