using System;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public class LambdaBasedCustomWriter : ICustomWriter
    {
        public Func<byte[], int, int, Task> WriteFunc { get; set; }

        public Func<Task> DisposeFunc { get; set; }

        public Task WriteBytes(byte[] data, int offset, int length)
        {
            var writeFunc = WriteFunc;
            if (writeFunc == null)
            {
                throw new MissingDependencyException("WriteFunc");
            }
            return writeFunc.Invoke(data, offset, length);
        }

        public Task CustomDispose()
        {
            return DisposeFunc?.Invoke() ?? Task.CompletedTask;
        }
    }
}
