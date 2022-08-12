using System;

namespace Kabomu.Mediator.Registry
{
    public interface IRegistryValueSource
    {
        Type ValueType { get; }

        object Get();
    }
}