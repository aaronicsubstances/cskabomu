using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal interface ITransfer
    {
        int RequestId { get; }
        void Abort(Exception exception);
        void ResetTimeout();
    }
}
