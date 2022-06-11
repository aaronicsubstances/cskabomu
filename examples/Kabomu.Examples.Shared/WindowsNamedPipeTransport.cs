using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class WindowsNamedPipeTransport : IQuasiHttpTransport
    {
        private readonly string _path;
        private readonly CancellationTokenSource _startCancelled;

        public WindowsNamedPipeTransport(string path)
        {
            _path = path;
            _startCancelled = new CancellationTokenSource();
            MaxChunkSize = 8192;
        }

        public int MaxChunkSize { get; set; }

        public bool DirectSendRequestProcessingEnabled => false;

        public KabomuQuasiHttpClient Upstream { get; set; }

        public UncaughtErrorCallback ErrorHandler { get; set; }

        public async void Start()
        {
            while (!_startCancelled.IsCancellationRequested)
            {
                try
                {
                    var pipeServer = new NamedPipeServerStream(_path, PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await pipeServer.WaitForConnectionAsync(_startCancelled.Token);
                    Upstream.OnReceive(pipeServer);
                }
                catch (Exception e)
                {
                    if (e is TaskCanceledException)
                    {
                        break;
                    }
                    else
                    {
                        ErrorHandler?.Invoke(e, "error encountered during receiving");
                    }
                }
            }
        }

        public void Stop()
        {
            _startCancelled.Cancel();
        }

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequest request,
            Action<Exception, IQuasiHttpResponse> cb)
        {
            throw new NotImplementedException();
        }

        public async void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            var path = (string)remoteEndpoint;
            var pipeClient = new NamedPipeClientStream(".", path, PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                await pipeClient.ConnectAsync(_startCancelled.Token);
            }
            catch (Exception e)
            {
                pipeClient.Dispose();
                cb.Invoke(e, null);
                return;
            }
            cb.Invoke(null, pipeClient);
        }

        public void OnReleaseConnection(object connection)
        {
            var pipeStream = (PipeStream)connection;
            pipeStream.Dispose();
        }

        public async void WriteBytes(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            var networkStream = (PipeStream)connection;
            try
            {
                await networkStream.WriteAsync(data, offset, length);
            }
            catch (Exception e)
            {
                cb.Invoke(e);
                return;
            }
            cb.Invoke(null);
        }

        public async void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            var networkStream = (PipeStream)connection;
            int bytesRead;
            try
            {
                bytesRead = await networkStream.ReadAsync(data, offset, length);
            }
            catch (Exception e)
            {
                cb.Invoke(e, 0);
                return;
            }
            cb.Invoke(null, bytesRead);
        }
    }
}
