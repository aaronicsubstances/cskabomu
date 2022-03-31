using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IMessageSink
    {/// <summary>
     /// Used to receive data to be written.
     /// </summary>
     /// <param name="data">source of data write</param>
     /// <param name="offset">offset in data array</param>
     /// <param name="length">length of data to be written</param>
     /// <param name="fallbackData">alternative source of data to use as fallback if data is null.</param>
     /// <param name="isMoreExpected">whether more writes are expected or this is the last write.</param>
     /// <param name="cb">callback which should be invoked to indicate success or failure of data write</param>
     /// <param name="cbState">Opaque object which must be supplied to callback</param>
        void OnDataWrite(byte[] data, int offset, int length, object fallbackPayload, 
            bool isMoreExpected, MessageSinkCallback cb, object cbState);

        /// <summary>
        /// Used to receive notice of end of entire write.
        /// <para>
        /// It is recommended that any disposable resources
        /// being used to serve writes should be disposed.
        /// </para>
        /// <para>
        /// It is also recommended that further data writes be failed
        /// immediately with the error used to end the entire write.
        /// </para>
        /// </summary>
        /// <param name="error">if not null then entire write failed; else entire write ended successfully.</param>
        void OnEndWrite(Exception error);
    }
}
