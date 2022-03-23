using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Rina
{
    public class Qpc101Facility : IQpcFacility
    {

        private readonly Dictionary<QpcIdentifier, Qpc101Data> _outgoingTransfers;

        public Qpc101Facility()
        {
            _outgoingTransfers = new Dictionary<QpcIdentifier, Qpc101Data>();
        }

        public int MinRetryBackoffPeriodMillis { get; set; }

        public int MaxRetryBackoffPeriodMillis { get; set; }
        
        public int DefaultTimeoutMillis { get; set; }

        public int MaxMessageSize { get; set; }

        public IEventLoopApi EventLoop { get; set; }

        public IRandomGenerator RandomGenerator { get; set; }

        public ErrorHandler ErrorCallback { get; set; }

        public IQpcFacility UpperLayer { get; set; }

        public IQpcFacility LowerLayer { get; set; }

        public void BeginReceive(INetworkAddress remoteAddress, IByteQueue message, Action<object, Exception, IByteQueue> cb,
            object cbState, IQpcReceiveOptions options)
        {
            throw new NotImplementedException();
        }

        public void BeginSend(INetworkAddress remoteAddress, IByteQueue message, Action<object, Exception, IByteQueue> cb, 
            object cbState, IQpcSendOptions options)
        {
            throw new NotImplementedException();
        }

        public void BeginReset(Exception causeOfReset, Action<Exception> cb)
        {
            throw new NotImplementedException();
        }
    }
}
