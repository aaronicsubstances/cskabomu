using Kabomu.Common.Abstractions;
using Kabomu.Common.Components;
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

        public ErrorHandler ErrorHandler { get; set; }

        public IQpcFacility UpperLayer { get; set; }

        public IQpcFacility LowerLayer { get; set; }

        public void BeginReceive(INetworkAddress remoteAddress, IByteQueue message, QpcReceiveCallback cb,
            object cbState, IQpcOptions options)
        {
            EventLoop.PostCallback(_ =>
            {
                ProcessReceive(remoteAddress, message, cb, cbState, options);
            }, null);
        }

        public void BeginSend(INetworkAddress remoteAddress, IByteQueue message, QpcSendCallback cb, 
            object cbState, IQpcOptions options)
        {
            throw new NotImplementedException();
        }

        public void BeginReset(Exception causeOfReset, Action<Exception> cb)
        {
            throw new NotImplementedException();
        }

        private void ProcessReceive(INetworkAddress remoteAddress, IByteQueue message, QpcReceiveCallback cb,
            object cbState, IQpcOptions options)
        {
            var messageWrapper = new CompositeByteQueue(message, null);
            byte version = messageWrapper.ReadUint8();
            byte pduType = messageWrapper.ReadUint8();
            int requestId = messageWrapper.ReadSint32be();
            byte errorCode = messageWrapper.ReadUint8();
            byte flags = messageWrapper.ReadUint8();
            UpperLayer.BeginReceive(remoteAddress, message, cb, cbState, options);
        }
    }
}
