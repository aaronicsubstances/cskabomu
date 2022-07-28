using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    public class TransportNotStartedException : Exception
    {
        public TransportNotStartedException():
            base("transport not started")
        {

        }
    }
}
