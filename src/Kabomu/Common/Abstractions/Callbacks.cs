using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public delegate void UncaughtErrorCallback(Exception error, string message);

    public delegate void MessageSinkCallback(object cbState, Exception error);

    public delegate void MessageSourceCallback(object cbState, Exception error,
        object data, bool hasMore);

    public delegate void MessageSinkCreationCallback(object cbState, Exception error, IMessageSink sink,
        Action<object, Exception> recvCb, object recvCbState);
}
