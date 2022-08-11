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
            var contextBuilder = new DefaultContextBuilder()
                .SetRequest(contextRequest)
                .SetResponse(contextResponse)
                .SetInitialHandlers(InitialHandlers)
                .SetFinalHandler(FinalHandler)
                .SetInitialReadonlyLocalRegistry(InitialReadonlyLocalRegistry)
                .SetReadonlyGlobalRegistry(ReadonlyGlobalRegistry);
            if (requestEnvironment != null)
            {
                var requestEnvBasedRegistry = new DefaultMutableRegistry();
                foreach (var entry in requestEnvironment)
                {
                    requestEnvBasedRegistry.Add(entry.Key, entry.Value);
                }
                if (contextBuilder.ReadonlyGlobalRegistry == null)
                {
                    contextBuilder.SetReadonlyGlobalRegistry(requestEnvBasedRegistry);
                }
                else
                {
                    contextBuilder.SetReadonlyGlobalRegistry(
                        contextBuilder.ReadonlyGlobalRegistry.Join(requestEnvBasedRegistry));
                }
            }
            _ = contextBuilder.Start();
            return tcs.Task;
        }
    }
}
