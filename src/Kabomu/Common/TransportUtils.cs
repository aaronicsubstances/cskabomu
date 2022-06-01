﻿using Kabomu.Internals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kabomu.Common
{
    public static class TransportUtils
    {
        public static void ReadBytesFully(IQuasiHttpTransport transport,
            object connection, byte[] data, int offset, int bytesToRead, Action<Exception> cb)
        {
            transport.ReadBytes(connection, data, offset, bytesToRead, (e, bytesRead) =>
                HandlePartialReadOutcome(transport, connection, data, offset, bytesToRead, e, bytesRead, cb));
        }

        private static void HandlePartialReadOutcome(IQuasiHttpTransport transport,
           object connection, byte[] data, int offset, int bytesToRead, Exception e, int bytesRead, Action<Exception> cb)
        {
            if (e != null)
            {
                cb.Invoke(e);
                return;
            }
            if (bytesRead < bytesToRead)
            {
                if (bytesRead <= 0)
                {
                    cb.Invoke(new Exception("end of read"));
                    return;
                }
                int newOffset = offset + bytesRead;
                int newBytesToRead = bytesToRead - bytesRead;
                transport.ReadBytes(connection, data, newOffset, newBytesToRead, (e, bytesRead) =>
                   HandlePartialReadOutcome(transport, connection, data, newOffset, newBytesToRead, e, bytesRead, cb));
            }
            else
            {
                cb.Invoke(null);
            }
        }

        public static void TransferBodyToTransport(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            IMutexApi mutex, Action<Exception> cb)
        {
            byte[] buffer = new byte[transport.MaxMessageOrChunkSize];
            body.OnDataRead(mutex, buffer, 0, buffer.Length, (e, bytesRead) =>
                HandleReadOutcome(transport, connection, body, mutex, buffer, e, bytesRead, cb));
        }

        private static void HandleWriteOutcome(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            IMutexApi mutex, byte[] buffer, Exception e, Action<Exception> cb)
        {
            if (e != null)
            {
                cb.Invoke(e);
                return;
            }
            body.OnDataRead(mutex, buffer, 0, buffer.Length, (e, bytesRead) =>
                HandleReadOutcome(transport, connection, body, mutex, buffer, e, bytesRead, cb));
        }

        private static void HandleReadOutcome(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            IMutexApi mutex, byte[] buffer, Exception e, int bytesRead, Action<Exception> cb)
        {
            if (e != null)
            {
                cb.Invoke(e);
                return;
            }
            if (bytesRead > 0)
            {
                transport.WriteBytes(connection, buffer, 0, bytesRead, e =>
                    HandleWriteOutcome(transport, connection, body, mutex, buffer, e, cb));
            }
            else
            {
                cb.Invoke(null);
            }
        }

        public static void ReadBodyToEnd(IQuasiHttpBody body, IMutexApi mutex, int maxChunkSize,
            Action<Exception, byte[]> cb)
        {
            var readBuffer = new byte[maxChunkSize];
            var byteStream = new MemoryStream();
            body.OnDataRead(mutex, readBuffer, 0, readBuffer.Length, (e, i) =>
                HandleReadOutcome2(body, mutex, readBuffer, byteStream, e, i, cb));
        }

        private static void HandleReadOutcome2(IQuasiHttpBody body, IMutexApi mutex, byte[] readBuffer,
            MemoryStream byteStream, Exception e, int bytesRead, Action<Exception, byte[]> cb)
        {
            if (e != null)
            {
                cb.Invoke(e, null);
                return;
            }
            if (bytesRead > 0)
            {
                byteStream.Write(readBuffer, 0, bytesRead);
                body.OnDataRead(mutex, readBuffer, 0, readBuffer.Length, (e, i) =>
                    HandleReadOutcome2(body, mutex, readBuffer, byteStream, e, i, cb));
            }
            else
            {
                body.OnEndRead(mutex, null);
                cb.Invoke(null, byteStream.ToArray());
            }
        }

        public static bool IsRequestPdu(byte[] data, int offset, int length)
        {
            if (length < 2)
            {
                throw new ArgumentException("too small to contain pdu type");
            }
            var pduType = data[offset + 1];
            return pduType == TransferPdu.PduTypeRequest || pduType == TransferPdu.PduTypeRequestChunkGet
                || pduType == TransferPdu.PduTypeRequestChunkRet;
        }

        public static int GetPduSequenceNumber(byte[] data, int offset, int length)
        {
            if (length < 7)
            {
                throw new ArgumentException("too small to contain sequence number");
            }
            return (int)ByteUtils.DeserializeUpToInt64BigEndian(data, offset + 3, 4);
        }
    }
}
