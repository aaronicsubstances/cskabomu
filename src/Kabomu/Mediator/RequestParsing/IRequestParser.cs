using Kabomu.Mediator.Handling;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.RequestParsing
{
    public interface IRequestParser
    {
        bool CanParse<T>(IContext context, object parseOpts);
        Task<T> Parse<T>(IContext context, object parseOpts);
    }
}
