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
        /// This function either returns a value from the
        /// <see cref="IQuasiHttpBody.Reader"/> property of a 
        /// quasi http body if the value is not null, or it sets up a
        /// custom reader (acceptable by
        /// <see cref="IOUtils.ReadBytes"/>) to return the bytes
        /// the body produces, by treating the body as an instance of
        /// <see cref="ISelfWritable"/>.
        /// </summary>
        /// <param name="body">the quasi http body</param>
        /// <returns>a reader which can be used to read bytes from the body</returns>
        public static object AsReader(this IQuasiHttpBody body)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }
            var reader = body.Reader;
            if (reader != null)
            {
                return reader;
            }
            var memoryPipe = new MemoryPipeCustomReaderWriter();
            // use Task.Run so as to prevent deadlock if writable is
            // doing synchronous writes.
            _ = Task.Run(() => ExhaustWritable(body, memoryPipe));
            return memoryPipe;
        }

        private static async Task ExhaustWritable(ISelfWritable writable,
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
