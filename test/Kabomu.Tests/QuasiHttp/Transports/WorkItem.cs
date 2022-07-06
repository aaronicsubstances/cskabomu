using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.QuasiHttp.Transports
{
    public class WorkItem
    {
        public bool IsWrite { get; set; }
        public string Dependency { get; set; }
        public string Tag { get; set; }
        public byte[] WriteData { get; set; }
        public int WriteOffset { get; set; }
        public int WriteLength { get; set; }
        public byte[] ReadBuffer { get; set; }
        public int ReadOffset { get; set; }
        public int BytesToRead { get; set; }
        public int ExpectedBytesRead { get; set; }
        public byte[] ExpectedReadData { get; set; }
        public Task WriteTask { get; set; }
        public Task<int> ReadTask { get; set; }

        public static List<WorkItem> CreateWorkItems()
        {
            var workItems = new List<WorkItem>();

            var writeData = ByteUtils.StringToBytes("ehlo from acres");
            var readData = new byte[34];
            workItems.Add(new WorkItem
            {
                IsWrite = true,
                WriteData = writeData,
                WriteOffset = 0,
                WriteLength = writeData.Length,
                Dependency = "0159b309-9224-4024-89f4-a90d66bf9f3c"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = false,
                ReadBuffer = readData,
                ReadOffset = 0,
                BytesToRead = readData.Length,
                ExpectedBytesRead = writeData.Length,
                ExpectedReadData = writeData,
                Tag = "0159b309-9224-4024-89f4-a90d66bf9f3c"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = false,
                ReadBuffer = new byte[0],
                ExpectedReadData = new byte[0]
            });
            workItems.Add(new WorkItem
            {
                IsWrite = true,
                WriteData = new byte[0]
            });
            workItems.Add(new WorkItem
            {
                IsWrite = false,
                ReadBuffer = readData,
                ReadOffset = 20,
                BytesToRead = 10,
                ExpectedBytesRead = 10,
                ExpectedReadData = ByteUtils.StringToBytes("ehlo from "),
                Dependency = "184e0a48-ff97-472c-9b8c-f84a4cccc637"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = true,
                WriteData = writeData,
                WriteOffset = 0,
                WriteLength = writeData.Length,
                Tag = "184e0a48-ff97-472c-9b8c-f84a4cccc637",
                Dependency = "cfefe0d8-2578-4376-a613-1819c5a99446"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = false,
                ReadBuffer = readData,
                ReadOffset = 2,
                BytesToRead = 1,
                ExpectedBytesRead = 1,
                ExpectedReadData = ByteUtils.StringToBytes("a")
            });
            workItems.Add(new WorkItem
            {
                IsWrite = true,
                WriteData = writeData,
                WriteOffset = 0,
                WriteLength = 4,
                Dependency = "f2547a5c-0e24-4ba4-91a5-128f9ef4eb27"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = true,
                WriteData = writeData,
                WriteOffset = 10,
                WriteLength = 5,
                Dependency = "83e659d1-0152-4a2d-a9c1-d1732a99325b"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = false,
                ReadBuffer = readData,
                ReadOffset = 20,
                BytesToRead = 10,
                ExpectedBytesRead = 4,
                ExpectedReadData = ByteUtils.StringToBytes("cres"),
                Tag = "cfefe0d8-2578-4376-a613-1819c5a99446"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = false,
                ReadBuffer = readData,
                ReadOffset = 0,
                BytesToRead = readData.Length,
                ExpectedBytesRead = 4,
                ExpectedReadData = ByteUtils.StringToBytes("ehlo"),
                Tag = "f2547a5c-0e24-4ba4-91a5-128f9ef4eb27"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = false,
                ReadBuffer = readData,
                ReadOffset = 0,
                BytesToRead = readData.Length,
                ExpectedBytesRead = 5,
                ExpectedReadData = ByteUtils.StringToBytes("acres"),
                Tag = "83e659d1-0152-4a2d-a9c1-d1732a99325b"
            });
            return workItems;
        }
    }
}
