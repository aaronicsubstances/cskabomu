﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Represents a facility that can provide timer-rleated services.
    /// </summary>
    public interface ITimerApi
    {
        /// <summary>
        /// Schedules a callback for execution in the future. Equivalent to setTimeout() in NodeJS
        /// </summary>
        /// <param name="cb">callback to run on timeout.</param>
        /// <param name="millis">delay in milliseconds from now after which to run callback</param>
        /// <returns>handle which can be used to cancel timeout request</returns>
        object SetTimeout(Action cb, int millis);

        /// <summary>
        /// Cancels the scheduling of a callback for future execution. Equivalent to clearTimeout() in NodeJS.
        /// </summary>
        /// <param name="timeoutHandle">handle generated by a previous call to SetTimeout()</param>
        void ClearTimeout(object timeoutHandle);
    }
}
