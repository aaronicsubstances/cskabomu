using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public static class EntityBodyExtensions
    {
        /// <summary>
        /// Get a value from the Reader property of an instance of
        /// <see cref="IQuasiHttpBody"/> class if the value is not null.
        /// And if the value is null, a reader is setup to return the bytes
        /// the body produces as a writable.
        /// </summary>
        /// <param name="body">the quasi http body</param>
        /// <returns>a reader which can be used to read bytes from the body</returns>
        public static object AsReader(this IQuasiHttpBody body)
        {
            var reader = body?.Reader;
            if (reader != null || body == null)
            {
                return reader;
            }
            var memoryPipe = new MemoryPipeCustomReaderWriter();
            // use Task.Run so as to prevent deadlock if writable is
            // doing synchronous writes.
            _ = Task.Run(() => ExhaustWritable(body, memoryPipe));
            return memoryPipe;
        }

        private static async Task ExhaustWritable(ICustomWritable writable,
            MemoryPipeCustomReaderWriter memoryPipe)
        {
            try
            {
                await writable.WriteBytesTo(memoryPipe);
                await memoryPipe.EndWrites();
            }
            catch (Exception e)
            {
                await memoryPipe.EndWrites(e);
            }
        }
    }
}
