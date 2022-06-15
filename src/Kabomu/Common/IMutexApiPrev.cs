using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public interface IMutexApiPrev
    {
        /// <summary>
        /// This method will characterize the means of writing thread-safe client code
        /// with this library.
        /// </summary>
        /// <param name="cb"></param>
        /// <param name="cbState"></param>
        void RunExclusively(Action<object> cb, object cbState);
    }
}
