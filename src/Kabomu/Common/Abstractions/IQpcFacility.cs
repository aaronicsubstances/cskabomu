using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IQpcFacility
    {
        IQpcFacility UpperLayer { get; }
        IQpcFacility LowerLayer { get; }
        void BeginReceive(INetworkAddress remoteAddress, IByteQueue message, Action<object, Exception, IByteQueue> cb,
            object cbState, IQpcReceiveOptions options);
        void BeginSend(INetworkAddress remoteAddress, IByteQueue message, Action<object, Exception, IByteQueue> cb,
            object cbState, IQpcSendOptions sendOptions);
        void BeginReset(Exception causeOfReset, Action<Exception> cb);
    }
}
