using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    public class TransportStoppageException : Exception
    {
        public TransportStoppageException() :
            base("transport stopped")
        {

        }
    }
}
