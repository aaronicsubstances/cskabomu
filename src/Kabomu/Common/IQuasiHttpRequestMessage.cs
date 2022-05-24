using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public interface IQuasiHttpRequestMessage
    {
        string Path { get; }
        Dictionary<string, List<string>> Headers { get; }
        IQuasiHttpBody Body { get; }
    }
}
