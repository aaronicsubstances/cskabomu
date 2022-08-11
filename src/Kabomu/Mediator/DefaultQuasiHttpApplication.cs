using Kabomu.Common;
using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Registry;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator
{
    public class DefaultQuasiHttpApplication : IQuasiHttpApplication
    {
        public IList<Handler> InitialHandlers { get; set; }
        public Handler FinalHandler { get; set; }
        public IRegistry InitialReadonlyLocalRegistry { get; set; }
        public IRegistry ReadonlyGlobalRegistry { get; set; }

        public Task<IQuasiHttpResponse> ProcessRequest(IQuasiHttpRequest request, IDictionary<string, object> requestEnvironment)
        {
            var contextRequest = new DefaultContextRequest(request);
            var tcs = new TaskCompletionSource<IQuasiHttpResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            var contextResponse = new DefaultContextResponse(new DefaultQuasiHttpResponse(), tcs);
            var context = new DefaultContext
            {
                Request = contextRequest,
                Response = contextResponse,
                InitialHandlers = InitialHandlers,
                FinalHandler = FinalHandler ?? new Handler(_ => Task.CompletedTask),
                InitialReadonlyLocalRegistry = InitialReadonlyLocalRegistry ?? EmptyRegistry.Instance,
                ReadonlyGlobalRegistry = ReadonlyGlobalRegistry,
            };
            if (context.InitialHandlers == null || context.InitialHandlers.Count == 0)
            {
                throw new MissingDependencyException("no initial handlers provided");
            }
            // add more readonly global constants
            var extraGlobalRegistry = new DefaultMutableRegistry();
            extraGlobalRegistry.Add<IContext>(context)
                .Add<IRequest>(contextRequest)
                .Add<IResponse>(contextResponse);
            if (requestEnvironment != null)
            {
                foreach (var entry in requestEnvironment)
                {
                    extraGlobalRegistry.Add(entry.Key, entry.Value);
                }
            }
            if (context.ReadonlyGlobalRegistry == null)
            {
                context.ReadonlyGlobalRegistry = extraGlobalRegistry;
            }
            else
            {
                context.ReadonlyGlobalRegistry = context.ReadonlyGlobalRegistry.Join(extraGlobalRegistry);
            }
            _ = context.Start();
            return tcs.Task;
        }
    }
}
