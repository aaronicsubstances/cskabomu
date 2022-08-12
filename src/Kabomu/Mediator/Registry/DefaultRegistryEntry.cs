using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    internal class DefaultRegistryEntry : IRegistryEntry
    {
        public object Key { get; set; }

        public Func<object> ValueGenerator { get; set; }
    }
}
