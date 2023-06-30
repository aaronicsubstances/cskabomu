using System;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public class LambdaBasedCustomReader : ICustomReader
    {
        private Func<byte[], int, int, Task<int>> _readFunc;
        private Func<Task> _disposeFunc;

        public LambdaBasedCustomReader(
            Func<byte[], int, int, Task<int>> readFunc,
            Func<Task> disposeFunc = null)
        {
            if (readFunc == null)
            {
                throw new ArgumentNullException(nameof(readFunc));
            }
            _readFunc = readFunc;
            _disposeFunc = disposeFunc;
        }

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            return _readFunc.Invoke(data, offset, length);
        }

        public Task CustomDispose()
        {
            return _disposeFunc?.Invoke() ?? Task.CompletedTask;
        }
    }
}
