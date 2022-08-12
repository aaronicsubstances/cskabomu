using System;

namespace Kabomu.Mediator.Registry
{
    internal interface IRegistryEntry
    {
        object Key { get; }
        Func<object> ValueGenerator { get; }
    }
}