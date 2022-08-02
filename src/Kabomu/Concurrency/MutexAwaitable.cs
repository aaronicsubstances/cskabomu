using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Implements awaitable protocol for <see cref="ConcurrencyExtensions.Synchronize(IMutexApi)"/>
    /// </summary>
    public readonly struct MutexAwaitable
    {
        private readonly IMutexApi _mutexApi;

        /// <summary>
        /// Constructs a new instance of the <see cref="MutexAwaitable"/> class.
        /// </summary>
        /// <param name="mutexApi">instance of <see cref="IMutexApi"/> which will be used to 
        /// perform mutual exclusion for execution of callbacks. Can be null.</param>
        public MutexAwaitable(IMutexApi mutexApi)
        {
            _mutexApi = mutexApi;
        }

        /// <summary>
        /// Returns a new instance of the <see cref="MutexAwaiter"/> class, giving it
        /// the <see cref="IMutexApi"/> instance received at construction time.
        /// </summary>
        public MutexAwaiter GetAwaiter()
        {
            return new MutexAwaiter(_mutexApi);
        }

        /// <summary>
        /// Implements awaiter protocol for <see cref="ConcurrencyExtensions.Synchronize(IMutexApi)"/>
        /// </summary>
        public readonly struct MutexAwaiter : INotifyCompletion
        {
            private readonly IMutexApi _mutexApi;

            /// <summary>
            /// Constructs a new instance of the <see cref="MutexAwaiter"/> class.
            /// </summary>
            /// <param name="mutexApi">instance of <see cref="IMutexApi"/> which will be used to 
            /// perform mutual exclusion for execution of callbacks. Can be null.</param>
            public MutexAwaiter(IMutexApi mutexApi)
            {
                _mutexApi = mutexApi;
            }

            /// <summary>
            /// Returns true to skip the call to the <see cref="OnCompleted(Action)"/> method, or
            /// makes use of members of the <see cref="IMutexContextFactory"/> and <see cref="IEventLoopApi"/>
            /// types as applicable depending on the type of the mutex api supplied at construction time.
            /// </summary>
            /// <remarks>
            ///     <list type="number">
            ///         <item>If mutex api is an instance of the <see cref="IMutexContextFactory"/> type,
            ///         then the negation of the <see cref="IMutexContextFactory.IsExclusiveRunRequired"/> property
            ///         is returned.</item>
            ///         <item>Else if mutex api is an event loop, then the value of the
            ///         <see cref="IEventLoopApi.IsInterimEventLoopThread"/> property is returned.</item>
            ///     </list>
            ///     Effectively this means that this property returns false if mutex api is a
            ///     mutex context factory which says exclusive run is required, or if mutex api is an
            ///     event loop which is currently not running in an interim event loop thread; and returns 
            ///     true otherwise.
            /// </remarks>
            public bool IsCompleted
            {
                get
                {
                    if (_mutexApi is IMutexContextFactory mutexContextFactory)
                    {
                        return !mutexContextFactory.IsExclusiveRunRequired;
                    }
                    else if (_mutexApi is IEventLoopApi eventLoopApi)
                    {
                        return eventLoopApi.IsInterimEventLoopThread;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            /// <summary>
            /// Executes a callback through the mutex api supplied at construction time.
            /// </summary>
            /// <remarks>
            /// The classical use case of this method is synchronization based on event loops.
            /// </remarks>
            /// <param name="continuation">the callback to run</param>
            public void OnCompleted(Action continuation)
            {
                _mutexApi.RunExclusively(continuation);
            }

            /// <summary>
            /// Returns an instance of the <see cref="IDisposable"/> type whose constructor and
            /// Dispose() method can be used to start and finish a synchronous mutual exclusion scheme.
            /// </summary>
            /// <remarks>
            /// The classical use case of this method is lock-based synchronization.
            /// </remarks>
            /// <returns>null or an instance of <see cref="IDisposable"/> type if mutex api supplied at 
            /// construction time is an instance of the <see cref="IMutexContextFactory"/> type</returns>
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
