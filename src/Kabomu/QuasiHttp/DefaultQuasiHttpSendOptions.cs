using Kabomu.Common;

namespace Kabomu.QuasiHttp
{
    public class DefaultQuasiHttpSendOptions : IQuasiHttpSendOptions
    {
        public int TimeoutMillis { get; set; }
    }
}