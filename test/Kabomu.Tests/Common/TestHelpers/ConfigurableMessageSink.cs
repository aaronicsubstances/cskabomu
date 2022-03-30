using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class ConfigurableMessageSink : IMessageSink
    {
        public delegate ConfigurableMessageSinkResult WriteDataChunkCallback(byte[] data, int offset, int length,
            object additionalPayload, bool isMoreExpected);
        public delegate void WriteEndCallback(Exception error);

        public WriteDataChunkCallback WriteDataChunkCallbackInstance { get; set; }
        public WriteEndCallback WriteEndCallbackInstance { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public OutputEventLogger Logger { get; set; }

        public void OnDataWrite(byte[] data, int offset, int length, object additionalPayload,
            bool isMoreExpected, MessageSinkCallback cb, object cbState)
        {
            Logger?.AppendSinkWriteDataLog(data, offset, length, additionalPayload, isMoreExpected);
            var res = WriteDataChunkCallbackInstance?.Invoke(data, offset, length, additionalPayload, isMoreExpected);
            if (res?.Delays != null)
            {
                foreach (int delay in res.Delays)
                {
                    EventLoop.ScheduleTimeout(delay, _ => cb.Invoke(cbState, res.DelayedError), null);
                }
            }
        }

        public void OnEndWrite(Exception error)
        {
            Logger?.AppendSinkOnEndWriteLog(error);
            WriteEndCallbackInstance?.Invoke(error);
        }
    }
}
