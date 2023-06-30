using System;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public class LambdaBasedCustomWriter : ICustomWriter
    {
        private Func<byte[], int, int, Task> _writeFunc;
        private Func<Task> _disposeFunc;

        public LambdaBasedCustomWriter(
            Func<byte[], int, int, Task> writeFunc,
            Func<Task> disposeFunc = null)
        {
            if (writeFunc == null)
            {
                throw new ArgumentNullException(nameof(writeFunc));
            }
            _writeFunc = writeFunc;
            _disposeFunc = disposeFunc;
        }

        public Task WriteBytes(byte[] data, int offset, int length)
        {
            return _writeFunc.Invoke(data, offset, length);
        }

        public Task CustomDispose()
        {
            return _disposeFunc?.Invoke() ?? Task.CompletedTask;
        }
    }
}
