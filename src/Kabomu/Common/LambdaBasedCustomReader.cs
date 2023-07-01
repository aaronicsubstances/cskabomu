using System;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public class LambdaBasedCustomReader : ICustomReader
    {
        public Func<byte[], int, int, Task<int>> ReadFunc { get; set; }
        
        public Func<Task> DisposeFunc { get; set; }

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            var readFunc = ReadFunc;
            if (readFunc == null)
            {
                throw new MissingDependencyException("ReadFunc");
            }
            return readFunc.Invoke(data, offset, length);
        }

        public Task CustomDispose()
        {
            return DisposeFunc?.Invoke() ?? Task.CompletedTask;
        }
    }
}
