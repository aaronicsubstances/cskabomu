using Kabomu.Common;
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
        public IRegistry InitialReadonlyLocalRegistry { get; set; }
        public IRegistry ReadonlyGlobalRegistry { get; set; }
        public IMutexApiFactory MutexApiFactory { get; set; }

        public async Task<IQuasiHttpResponse> ProcessRequest(IQuasiHttpRequest request, IDictionary<string, object> requestEnvironment)
        {
            var contextRequest = new DefaultContextRequest(request, requestEnvironment);
            var tcs = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var contextResponse = new DefaultContextResponse(new DefaultQuasiHttpResponse(), tcs);
            var context = new DefaultContext
            {
                Request = contextRequest,
                Response = contextResponse,
                InitialHandlers = InitialHandlers,
                InitialReadonlyLocalRegistry = InitialReadonlyLocalRegistry,
                ReadonlyGlobalRegistry = ReadonlyGlobalRegistry,
            };
            if (context.InitialHandlers == null || context.InitialHandlers.Count == 0)
            {
                throw new MissingDependencyException("no initial handlers provided");
            }
            if (context.InitialReadonlyLocalRegistry == null)
            {
                context.InitialReadonlyLocalRegistry = EmptyRegistry.Instance;
            }
            if (context.ReadonlyGlobalRegistry == null)
            {
                context.ReadonlyGlobalRegistry = EmptyRegistry.Instance;
            }

            var mutexApiTask = MutexApiFactory?.Create();
            if (mutexApiTask != null)
            {
                context.MutexApi = await mutexApiTask;
            }

            await context.Start();

            return await tcs.Task;
        }
    }
}
