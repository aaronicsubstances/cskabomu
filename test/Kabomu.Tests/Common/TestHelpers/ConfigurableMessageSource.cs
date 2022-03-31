using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class ConfigurableMessageSource : IMessageSource
    {
        public delegate ConfigurableMessageSourceResult ReadDataChunkCallback();
        public delegate void ReadEndCallback(Exception error);

        public ReadDataChunkCallback ReadDataChunkCallbackInstance { get; set; }
        public ReadEndCallback ReadEndCallbackInstance { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public OutputEventLogger Logger { get; set; }

        public void OnDataRead(MessageSourceCallback cb, object cbState)
        {
            Logger?.AppendSourceReadDataLog();
            var res = ReadDataChunkCallbackInstance?.Invoke();
            if (res?.Delays != null)
            {
                foreach (int delay in res.Delays)
                {
                    EventLoop.ScheduleTimeout(delay, _ => cb.Invoke(cbState, res.DelayedError, res.Data,
                        res.Offset, res.Length, res.FallbackPayload, res.HasMore), null);
                }
            }
        }

        public void OnEndRead(Exception error)
        {
            Logger?.AppendSourceOnEndReadLog(error);
            ReadEndCallbackInstance?.Invoke(error);
        }
    }
}
