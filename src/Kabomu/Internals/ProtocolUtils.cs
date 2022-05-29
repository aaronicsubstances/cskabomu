using Kabomu.Common;
using System;

namespace Kabomu.Internals
{
    internal class ProtocolUtils
    {
        public static Action<Action<bool>> CreateCancellationEnquirer(IMutexApi mutex, 
            STCancellationIndicator cancellationIndicator)
        {
            return cb =>
            {
                mutex.RunExclusively(_ =>
                {
                    cb.Invoke(cancellationIndicator.Cancelled);
                }, null);
            };
        }
    }
}