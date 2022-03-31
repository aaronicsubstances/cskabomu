﻿using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class ConfigurableMessageSinkFactory : IMessageSinkFactory
    {
        public delegate ConfigurableSinkCreationResult CreateMessageSinkCallback();
        public CreateMessageSinkCallback CreateMessageSinkCallbackInstance { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public OutputEventLogger Logger { get; set; }

        public void CreateMessageSink(MessageSinkCreationCallback cb, object cbState)
        {
            Logger?.AppendSinkCreationLog();
            var res = CreateMessageSinkCallbackInstance?.Invoke();
            if (res?.Delays != null)
            {
                foreach (int delay in res.Delays)
                {
                    EventLoop.ScheduleTimeout(delay, _ => cb.Invoke(cbState, res.DelayedError,
                        res.Sink, res.CancellationIndicator, res.RecvCb, res.RecvCbState), null);
                }
            }
        }
    }
}
