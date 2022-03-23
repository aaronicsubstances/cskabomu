using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public delegate void ErrorHandler(Exception error, string message);

    public delegate void QpcSendCallback(object cbState, Exception error, IByteQueue message);

    public delegate void QpcReceiveCallback(object cbState, Exception error, IByteQueue message);
}
