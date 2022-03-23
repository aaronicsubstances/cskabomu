using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IQpcFacility
    {
        ErrorHandler ErrorHandler { get; set; }
        IQpcFacility UpperLayer { get; set; }
        IQpcFacility LowerLayer { get; set; }
        void BeginReceive(INetworkAddress remoteAddress, IByteQueue message, QpcReceiveCallback cb,
            object cbState, IQpcOptions options);
        void BeginSend(INetworkAddress remoteAddress, IByteQueue message, QpcSendCallback cb,
            object cbState, IQpcOptions sendOptions);
        void BeginReset(Exception causeOfReset, Action<Exception> cb);
    }
}
