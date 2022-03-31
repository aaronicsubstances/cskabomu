using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Components
{
    public class DefaultTransferEndpoint : ITransferEndpoint
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public object AdditionalData { get; set; }

        public override string ToString()
        {
            return (Name ?? "") + ":" + Id;
        }
    }
}
