using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public interface IQuasiHttpServer
    {
        UncaughtErrorCallback ErrorHandler { get; set; }
        int DefaultTimeoutMillis { get; set; }
        IQuasiHttpApplication Application { get; set; }
        IQuasiHttpTransport Transport { get; set; }
        Task Start();
        Task Stop();
    }
}
