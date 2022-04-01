using Kabomu.Common.Abstractions;
using Kabomu.Common.Components;
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

        public void AppendTransferError(Exception error, string extra)
        {
            var log = CreateOnErrorLog(error?.Message, extra);
            Logs.Add($"{EventLoop.CurrentTimestamp}:{log}");
        }

        public void AppendEventLoopError(Exception error, string extra)
        {
            var log = CreateEventLoopErrorLog(error?.Message, extra);
            Logs.Add($"{EventLoop.CurrentTimestamp}:{log}");
        }

        public void AppendCallbackLog(Exception ex)
        {
            var log = CreateCallbackLog(ex?.Message);
            Logs.Add($"{EventLoop.CurrentTimestamp}:{log}");
        }

        public void AppendOnReceivePduLog(object connectionHandle, byte version, byte pduType, byte flags, byte errorCode,
            long messageId, byte[] data, int offset, int length, object fallbackPayload,
            ICancellationIndicator cancellationIndicator)
        {
            var log = CreateOnReceivePduLog(connectionHandle, version, pduType, flags, errorCode, messageId, data, 
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

        public void AppendSinkCreationLog()
        {
            var log = CreateSinkCreationLog();
            Logs.Add($"{EventLoop.CurrentTimestamp}:{log}");
        }

        public static string CreateOnErrorLog(string errorMessage, string extra)
        {
            errorMessage = ReduceErrorMessageToErrorCode(errorMessage);
            return "TransferError" +
                $"(" +
                $"{errorMessage}," +
                $"{extra}" +
                $")";
        }

        public static string CreateEventLoopErrorLog(string errorMessage, string extra)
        {
            errorMessage = ReduceErrorMessageToErrorCode(errorMessage);
            return "EvError" +
                $"(" +
                $"{errorMessage}," +
                $"{extra}" +
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
            if (errorMessage != null)
            {
                int errorCodeDelimIdx = errorMessage.IndexOf(":");
                if (errorCodeDelimIdx != -1)
                {
                    errorMessage = errorMessage.Substring(0, errorCodeDelimIdx);
                }
            }
            return errorMessage;
        }

        public static string CreateOnReceivePduLog(object connectionHandle, byte version, byte pduType, byte flags, byte errorCode,
            long messageId, byte[] data, int offset, int length, object fallbackPayload,
            bool? cancelled)
        {
            string message = null;
            if (data != null)
            {
                message = ByteUtils.BytesToString(data, offset, length);
            }
            return "TransferChunk" +
                $"(" +
                $"{connectionHandle}," +
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
                $"{isMoreExpected.ToString().ToLower()}" +
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

        public static string CreateSinkCreationLog()
        {
            return "SnkCreate()";
        }
    }
}
