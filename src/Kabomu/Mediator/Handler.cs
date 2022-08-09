using System;
using System.Threading.Tasks;

namespace Kabomu.Mediator
{
    public delegate Task Handler(IContext context);
}