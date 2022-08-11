using Kabomu.Common;
using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    public class DefaultContextBuilder
    {
        public IRequest Request { get; private set; }

        public IResponse Response { get; private set; }

        public IRegistry InitialReadonlyLocalRegistry { get; private set; }

        public IRegistry ReadonlyGlobalRegistry { get; private set; }

        public IList<Handler> InitialHandlers { get; private set; }

        public Handler FinalHandler { get; private set; }

        public DefaultContextBuilder SetRequest(IRequest value)
        {
            Request = value;
            return this;
        }

        public DefaultContextBuilder SetResponse(IResponse value)
        {
            Response = value;
            return this;
        }

        public DefaultContextBuilder SetInitialReadonlyLocalRegistry(IRegistry value)
        {
            InitialReadonlyLocalRegistry = value;
            return this;
        }

        public DefaultContextBuilder SetReadonlyGlobalRegistry(IRegistry value)
        {
            ReadonlyGlobalRegistry = value;
            return this;
        }

        public DefaultContextBuilder SetInitialHandlers(IList<Handler> handlers)
        {
            InitialHandlers = handlers;
            return this;
        }

        public DefaultContextBuilder SetFinalHandler(Handler handler)
        {
            FinalHandler = handler;
            return this;
        }

        public Task Start()
        {
            if (Request == null)
            {
                throw new MissingDependencyException("null request");
            }
            if (Response == null)
            {
                throw new MissingDependencyException("null response");
            }
            if (InitialHandlers == null || InitialHandlers.Count == 0)
            {
                throw new MissingDependencyException("no initial handlers provided");
            }
            var context = new DefaultContext
            {
                Request = Request,
                Response = Response,
                InitialHandlers = InitialHandlers,
                FinalHandler = FinalHandler ?? new Handler(_ => Task.CompletedTask),
                InitialReadonlyLocalRegistry = InitialReadonlyLocalRegistry ?? EmptyRegistry.Instance,
                ReadonlyGlobalRegistry = ReadonlyGlobalRegistry ?? EmptyRegistry.Instance,
            };
            // add more readonly global constants.
            context.ReadonlyGlobalRegistry = context.ReadonlyGlobalRegistry.Join(
                new MultiValueRegistry(new List<object> { Request, Response, context }));
            return context.Start();
        }
    }
}
