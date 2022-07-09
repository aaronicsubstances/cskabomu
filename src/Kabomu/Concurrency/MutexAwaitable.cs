using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kabomu.Concurrency
{
    public readonly struct MutexAwaitable
    {
        private readonly IMutexApi _mutexApi;

        public MutexAwaitable(IMutexApi mutexApi)
        {
            _mutexApi = mutexApi;
        }

        public MutexAwaiter GetAwaiter()
        {
            return new MutexAwaiter(_mutexApi);
        }

        public readonly struct MutexAwaiter : INotifyCompletion
        {
            private readonly IMutexApi _mutexApi;

            public MutexAwaiter(IMutexApi mutexApi)
            {
                _mutexApi = mutexApi;
            }

            public bool IsCompleted
            {
                get
                {
                    if (_mutexApi is IMutexContextFactory mutexContextFactory)
                    {
                        return !mutexContextFactory.IsExclusiveRunRequired;
                    }
                    else if (_mutexApi is ISynchronizedEventLoopApi eventLoopApi)
                    {
                        return eventLoopApi.IsInterimEventLoopThread;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            public void OnCompleted(Action continuation)
            {
                _mutexApi.RunExclusively(continuation);
            }

            public IDisposable GetResult()
            {
                if (_mutexApi is IMutexContextFactory mutexContextFactory)
                {
                    return mutexContextFactory.CreateMutexContext();
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
