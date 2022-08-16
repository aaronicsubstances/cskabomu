using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public interface IRegistryKeyPattern
    {
        bool IsMatch(object input);
    }
}
