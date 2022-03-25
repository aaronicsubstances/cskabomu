using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IMessageSource
    {
        /// <summary>
        /// Used to retrieve data to be read.
        /// </summary>
        /// <param name="cb">callback which should be invoked to indicate success or failure of data read</param>
        /// <param name="cbState">Opaque object which must be supplied to callback</param>
        void OnDataRead(MessageSourceCallback cb, object cbState);

        /// <summary>
        /// Used to receive notice of end of entire read.
        /// <para>
        /// It is recommended that any disposable resources
        /// being used to serve reads should be disposed.
        /// </para>
        /// <para>
        /// It is also recommended that further data reads should be
        /// failed immediately with the error used to end the entire read.
        /// </para>
        /// </summary>
        /// <param name="error">if not null then entire read failed; else entire read ended successfully.</param>
        void OnEndRead(Exception error);
    }
}
