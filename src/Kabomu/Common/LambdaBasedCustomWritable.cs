using System;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public class LambdaBasedCustomWritable : ICustomWritable
    {
        private Func<ICustomWriter, Task> _writableFunc;
        private Func<Task> _disposeFunc;

        public LambdaBasedCustomWritable(
            Func<ICustomWriter, Task> writableFunc,
            Func<Task> disposeFunc = null)
        {
            if (writableFunc == null)
            {
                throw new ArgumentNullException(nameof(writableFunc));
            }
            _writableFunc = writableFunc;
            _disposeFunc = disposeFunc;
        }

        public Task WriteBytesTo(ICustomWriter writer)
        {
            return _writableFunc.Invoke(writer);
        }

        public Task CustomDispose()
        {
            return _disposeFunc?.Invoke() ?? Task.CompletedTask;
        }
    }
}
