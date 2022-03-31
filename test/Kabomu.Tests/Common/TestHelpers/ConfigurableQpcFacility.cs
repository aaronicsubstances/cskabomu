using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class ConfigurableQpcFacility : IQpcFacility
    {
        public delegate ConfigurableSendPduResult SendPduCallback(object connectionHandle,
            byte version, byte pduType, byte flags, 
            byte errorCode, long messageId,
            byte[] data, int offset, int length, object fallbackPayload,
            ICancellationIndicator cancellationIndicator);
        public SendPduCallback SendPduCallbackInstance { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public OutputEventLogger Logger { get; set; }

        public void BeginSendPdu(object connectionHandle, byte version, byte pduType, byte flags, byte errorCode, long messageId, 
            byte[] data, int offset, int length, object fallbackPayload, ICancellationIndicator cancellationIndicator, 
            Action<object, Exception> cb, object cbState)
        {
            Logger?.AppendOnReceivePduLog(connectionHandle, version, pduType, flags, errorCode, messageId,
                data, offset, length, fallbackPayload, cancellationIndicator);
            var res = SendPduCallbackInstance?.Invoke(connectionHandle, version, pduType, flags, errorCode, messageId,
                data, offset, length, fallbackPayload, cancellationIndicator);
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
