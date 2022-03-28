using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IQpcFacility
    {
        void BeginSend(byte version, byte pduType, byte flags, byte errorCode,
            long messageId, byte[] data, int offset, int length, object alternativePayload,
            ICancellationIndicator cancellationIndicator, Action<object, Exception> cb, object cbState);
    }
}
