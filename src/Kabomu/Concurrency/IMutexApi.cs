using System;

namespace Kabomu.Concurrency
{
    public interface IMutexApi
    {
        bool IsExclusiveRunRequired { get; }

        /// <summary>
        /// This method will characterize the means of writing thread-safe client code
        /// with this library.
        /// </summary>
        /// <param name="cb"></param>
        void RunExclusively(Action cb);

        IDisposable CreateMutexContextManager();
    }
}