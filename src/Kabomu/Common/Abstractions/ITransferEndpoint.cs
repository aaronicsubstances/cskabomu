using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface ITransferEndpoint
    {
        int Id { get; }
        string Name { get; }
    }
}
