using Kabomu.Common.Abstractions;
using Kabomu.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Kabomu.Tests.Common.TestHelpers
{
    public class OutputEventLogger
    {
        public List<string> Logs { get; set; }
        public IEventLoopApi EventLoop { get; set; }

        public void AssertEqual(List<string> expectedLogs, ITestOutputHelper outputHelper)
        {
            int minCount = Math.Min(Logs.Count, expectedLogs.Count);
            try
            {
                Assert.Equal(expectedLogs.GetRange(0, minCount), Logs.GetRange(0, minCount));
                Assert.Equal(expectedLogs.Count, Logs.Count);
            }
            catch (Exception)
            {
                if (outputHelper != null)
                {
                    outputHelper.WriteLine("Expected:");
                    outputHelper.WriteLine(string.Join(Environment.NewLine, expectedLogs));
                    outputHelper.WriteLine("Actual:");
                    outputHelper.WriteLine(string.Join(Environment.NewLine, Logs));
                }
                throw;
            }
        }

        public void AppendOnError(Exception error)
        {
            var log = CreateOnErrorLog(error?.Message);
            Logs.Add($"{EventLoop.CurrentTimestamp}:{log}");
        }

        public void AppendEventLoopError(Exception error)
        {
            var log = CreateEventLoopErrorLog(error?.Message);
            Logs.Add($"{EventLoop.CurrentTimestamp}:{log}");
        }

        public void AppendCallbackLog(Exception ex)
        {
            var log = CreateCallbackLog(ex?.Message);
            Logs.Add($"{EventLoop.CurrentTimestamp}:{log}");
        }

        public void AppendOnReceivePduLog(ITransferEndpoint remoteEndpoint, byte version, byte pduType, byte flags, byte errorCode,
            long messageId, byte[] data, int offset, int length, object fallbackPayload,
            ICancellationIndicator cancellationIndicator)
        {
            var log = CreateOnReceivePduLog(remoteEndpoint, version, pduType, flags, errorCode, messageId, data, 
                offset, length, fallbackPayload, cancellationIndicator?.Cancelled);
            Logs.Add($"{EventLoop.CurrentTimestamp}:{log}");
        }

        public void AppendSinkWriteDataLog(byte[] data, int offset, int length, 
            object fallbackPayload, bool isMoreExpected)
        {
            var log = CreateSinkWriteDataLog(data, offset, length, fallbackPayload, isMoreExpected);
            Logs.Add($"{EventLoop.CurrentTimestamp}:{log}");
        }

        public void AppendSinkOnEndWriteLog(Exception error)
        {
            var log = CreateSinkOnEndWriteLog(error?.Message);
            Logs.Add($"{EventLoop.CurrentTimestamp}:{log}");
        }

        public void AppendSourceReadDataLog()
        {
            var log = CreateSourceReadDataLog();
            Logs.Add($"{EventLoop.CurrentTimestamp}:{log}");
        }

        public void AppendSourceOnEndReadLog(Exception error)
        {
            var log = CreateSourceOnEndReadLog(error?.Message);
            Logs.Add($"{EventLoop.CurrentTimestamp}:{log}");
        }

        public void AppendSinkCreationLog(ITransferEndpoint remoteEndpoint)
        {
            var log = CreateSinkCreationLog(remoteEndpoint);
            Logs.Add($"{EventLoop.CurrentTimestamp}:{log}");
        }

        public static string CreateOnErrorLog(string errorMessage)
        {
            errorMessage = ReduceErrorMessageToErrorCode(errorMessage);
            return "Error" +
                $"(" +
                $"{errorMessage}" +
                $")";
        }

        public static string CreateEventLoopErrorLog(string errorMessage)
        {
            errorMessage = ReduceErrorMessageToErrorCode(errorMessage);
            return "EvError" +
                $"(" +
                $"{errorMessage}" +
                $")";
        }

        public static string CreateCallbackLog(string errorMessage)
        {
            errorMessage = ReduceErrorMessageToErrorCode(errorMessage);
            return "Cb" +
                $"(" +
                $"{errorMessage}" +
                $")";
        }

        private static string ReduceErrorMessageToErrorCode(string errorMessage)
        {
            int errorCodeDelimIdx = errorMessage.IndexOf(":");
            if (errorCodeDelimIdx != -1)
            {
                errorMessage = errorMessage.Substring(0, errorCodeDelimIdx);
            }
            return errorMessage;
        }

        public static string CreateOnReceivePduLog(ITransferEndpoint remoteEndpoint, byte version, byte pduType, byte flags, byte errorCode,
            long messageId, byte[] data, int offset, int length, object fallbackPayload,
            bool? cancelled)
        {
            var message = ByteUtils.BytesToString(data, offset, length);
            return "QpcReceive" +
                $"(" +
                $"{remoteEndpoint}," +
                $"{version}," +
                $"{pduType}," +
                $"{flags}," +
                $"{errorCode}," +
                $"{messageId}," +
                $"{message}," +
                $"{fallbackPayload}," +
                $"{cancelled?.ToString()?.ToLower()}" +
                $")";
        }

        public static string CreateSinkWriteDataLog(byte[] data, int offset, int length,
            object fallbackPayload, bool isMoreExpected)
        {
            var message = ByteUtils.BytesToString(data, offset, length);
            return "SnkData" +
                $"(" +
                $"{message}," +
                $"{fallbackPayload}," +
                $"{isMoreExpected}" +
                $")";
        }

        public static string CreateSinkOnEndWriteLog(string errorMessage)
        {
            errorMessage = ReduceErrorMessageToErrorCode(errorMessage);
            return "SnkEnd" +
                $"(" +
                $"{errorMessage}" +
                $")";
        }

        public static string CreateSourceReadDataLog()
        {
            return "SrcData()";
        }

        public static string CreateSourceOnEndReadLog(string errorMessage)
        {
            errorMessage = ReduceErrorMessageToErrorCode(errorMessage);
            return "SrcEnd" +
                $"(" +
                $"{errorMessage}" +
                $")";
        }

        public static string CreateSinkCreationLog(ITransferEndpoint remoteEndpoint)
        {
            return $"AcSnkCreate({remoteEndpoint})";
        }
    }
}
