using System;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    public delegate Task Handler(IContext context);
}