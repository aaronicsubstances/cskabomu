using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Tests.Common.TestHelpers
{

    public class ConfigurableQpcFacility : IQpcFacility
    {
        public delegate ConfigurableSendPduResult SendPduCallback(byte version, byte pduType, byte flags, 
            byte errorCode, long messageId,
            byte[] data, int offset, int length, object additionalPayload,
            ICancellationIndicator cancellationIndicator);
        public SendPduCallback SendPduCallbackInstance { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public OutputEventLogger Logger { get; set; }

        public void BeginSendPdu(byte version, byte pduType, byte flags, byte errorCode, long messageId, 
            byte[] data, int offset, int length, object additionalPayload, ICancellationIndicator cancellationIndicator, 
            Action<object, Exception> cb, object cbState)
        {
            Logger?.AppendOnReceivePduLog(version, pduType, flags, errorCode, messageId,
                data, offset, length, additionalPayload, cancellationIndicator);
            var res = SendPduCallbackInstance?.Invoke(version, pduType, flags, errorCode, messageId,
                data, offset, length, additionalPayload, cancellationIndicator);
            if (res?.Delays != null)
            {
                foreach (int delay in res.Delays)
                {
                    EventLoop.ScheduleTimeout(delay, _ => cb.Invoke(cbState, res.DelayedError), null);
                }
            }
        }
    }
}
