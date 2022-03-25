using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IQpcFacility
    {
        void BeginSend(byte version, byte pduType, byte flags, byte errorCode,
            long messageIdPart1, long messageIdPart2, object payload,
            ICancellationHandle cancellationHandle, Action<object, Exception> cb, object cbState);
    }
}
