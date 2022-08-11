using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    public class DefaultContextBuilder
    {
        public IRequest Request { get; private set; }

        public DefaultContextBuilder SetRequest(IRequest value)
        {
            Request = value;
            return this;
        }

        public IResponse Response { get; private set; }

        public DefaultContextBuilder SetResponse(IResponse value)
        {
            Response = value;
            return this;
        }

        public IRegistry InitialReadonlyLocalRegistry { get; private set; }

        public DefaultContextBuilder SetInitialReadonlyLocalRegistry(IRegistry value)
        {
            InitialReadonlyLocalRegistry = value;
            return this;
        }

        public IRegistry ReadonlyGlobalRegistry { get; private set; }

        public DefaultContextBuilder SetReadonlyGlobalRegistry(IRegistry value)
        {
            ReadonlyGlobalRegistry = value;
            return this;
        }

        public Handler[] InitialHandlers { get; private set; }

        public DefaultContextBuilder SetInitialHandlers(params Handler[] handlers)
        {
            InitialHandlers = handlers;
            return this;
        }

        public Handler FinalHandler { get; private set; }

        public DefaultContextBuilder SetFinalHandler(Handler handler)
        {
            FinalHandler = handler;
            return this;
        }

        public Task Start()
        {
            var context = new DefaultContext
            {
                Request = Request,
                Response = Response,
                InitialReadonlyLocalRegistry = InitialReadonlyLocalRegistry,
                ReadonlyGlobalRegistry = ReadonlyGlobalRegistry,
                InitialHandlers = InitialHandlers,
                FinalHandler = FinalHandler
            };
            // add more readonly global constants.
            context.ReadonlyGlobalRegistry = context.ReadonlyGlobalRegistry.Join(
                new MultiValueRegistry(new List<object> { Request, Response, context }));
            return context.Start();
        }
    }
}
