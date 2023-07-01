using System;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public class LambdaBasedCustomWritable : ICustomWritable
    {
        public Func<ICustomWriter, Task> WritableFunc { get; set; }

        public Func<Task> DisposeFunc { get; set; }

        public Task WriteBytesTo(ICustomWriter writer)
        {
            var writableFunc = WritableFunc;
            if (writableFunc == null)
            {
                throw new MissingDependencyException("WritableFunc");
            }
            return writableFunc.Invoke(writer);
        }

        public Task CustomDispose()
        {
            return DisposeFunc?.Invoke() ?? Task.CompletedTask;
        }
    }
}
