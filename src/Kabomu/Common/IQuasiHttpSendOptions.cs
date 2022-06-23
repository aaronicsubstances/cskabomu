using System.Collections.Generic;

namespace Kabomu.Common
{
    public interface IQuasiHttpSendOptions
    {
        int TimeoutMillis { get; }
        IDictionary<string, object> RequestEnvironment { get; }
    }
}