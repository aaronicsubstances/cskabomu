using Kabomu.Common;
using Kabomu.Tests.Shared.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class MemoryPipeCustomReaderWriterTest
    {
        [Theory]
        [InlineData("")]
        [InlineData("achievers")]
        [InlineData("database.")]
        public async Task TestReadingAndWriting(string expected)
        {
            // arrange
            var initialReader = new RandomizedReadSizeBufferReader(
                ByteUtils.StringToBytes(expected));
            var instance = new MemoryPipeCustomReaderWriter();

            // act
            var t1 = IOUtils.ReadAllBytes(instance);
            var f2 = async () =>
            {
                await IOUtils.CopyBytes(initialReader, instance);
                await instance.EndWrites();
            };
            var t2 = f2();
            // just in case error causes t1 or t2 to hang forever,
            // impose timeout
            var delayTask = Task.Delay(3000);
            var firstTask = await Task.WhenAny(delayTask, Task.WhenAll(t1, t2));

            // assert
            string actual = null;
            if (firstTask != delayTask)
            {
                // let any exceptions bubble up.
                await t2;
                var actualBytes = await t1;
                actual = ByteUtils.BytesToString(actualBytes);
            }
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestInternals1()
        {
            var instance = new MemoryPipeCustomReaderWriter();
            byte[] readBuffer = new byte[3];
            var readTask = instance.ReadBytes(readBuffer, 0, readBuffer.Length);

            await instance.WriteBytes(new byte[] { 4, 5, 6 }, 0, 3);
            int readLen = await readTask;
            Assert.Equal(3, readLen);
            ComparisonUtils.CompareData(new byte[] { 4, 5, 6 }, 0, 3,
                readBuffer, 0, readLen);

            readTask = instance.ReadBytes(readBuffer, 0, 0);

            await instance.WriteBytes(new byte[10], 0, 0);

            var delayTask = Task.Delay(200);
            Assert.Same(delayTask, await Task.WhenAny(readTask, delayTask));

            var writeTask = instance.WriteBytes(new byte[] { 0, 2, 4, 6, 8, 10 },
                1, 4);
            readLen = await readTask;
            Assert.Equal(0, readLen);

            readLen = await instance.ReadBytes(readBuffer, 0, 3);
            Assert.Equal(3, readLen);
            ComparisonUtils.CompareData(new byte[] { 2, 4, 6 }, 0, 3,
                readBuffer, 0, readLen);

            readLen = await instance.ReadBytes(readBuffer, 1, 2);
            Assert.Equal(1, readLen);
            ComparisonUtils.CompareData(new byte[] { 8 }, 0, 1,
                readBuffer, 1, readLen);

            await writeTask;

            // Now test for errors
            writeTask = instance.WriteBytes(new byte[] { 9, 7 }, 0, 2);

            await instance.EndWrites(new NotImplementedException());
            await instance.EndWrites(new NotSupportedException()); // should have no effect

            await Assert.ThrowsAsync<NotImplementedException>(
                () => writeTask);

            await Assert.ThrowsAsync<NotImplementedException>(
                () => instance.ReadBytes(readBuffer, 0, 1));

            await Assert.ThrowsAsync<NotImplementedException>(
                () => instance.WriteBytes(new byte[10], 0, 4));
        }

        [Fact]
        public async Task TestInternals2()
        {
            var instance = new MemoryPipeCustomReaderWriter();
            byte[] readBuffer = new byte[3];
            var readTask = instance.ReadBytes(readBuffer, 0, readBuffer.Length);

            await instance.WriteBytes(new byte[] { 4, 5, 6 }, 0, 3);
            int readLen = await readTask;
            Assert.Equal(3, readLen);
            ComparisonUtils.CompareData(new byte[] { 4, 5, 6 }, 0, 3,
                readBuffer, 0, readLen);

            readTask = instance.ReadBytes(readBuffer, 0, 0);

            await instance.WriteBytes(new byte[10], 0, 0);

            var delayTask = Task.Delay(200);
            Assert.Same(delayTask, await Task.WhenAny(readTask, delayTask));

            var writeTask = instance.WriteBytes(new byte[] { 0, 2, 4, 6, 8, 10 },
                1, 4);
            readLen = await readTask;
            Assert.Equal(0, readLen);

            readLen = await instance.ReadBytes(readBuffer, 0, 3);
            Assert.Equal(3, readLen);
            ComparisonUtils.CompareData(new byte[] { 2, 4, 6 }, 0, 3,
                readBuffer, 0, readLen);

            readLen = await instance.ReadBytes(readBuffer, 1, 2);
            Assert.Equal(1, readLen);
            ComparisonUtils.CompareData(new byte[] { 8 }, 0, 1,
                readBuffer, 1, readLen);

            await writeTask;

            // Now test for errors
            readTask = instance.ReadBytes(readBuffer, 0, 2);

            await instance.EndWrites(new NotSupportedException());
            await instance.EndWrites(); // should have no effect

            await Assert.ThrowsAsync<NotSupportedException>(
                () => readTask);

            await Assert.ThrowsAsync<NotSupportedException>(
                () => instance.ReadBytes(readBuffer, 0, 1));

            await Assert.ThrowsAsync<NotSupportedException>(
                () => instance.WriteBytes(new byte[10], 0, 4));
        }

        [Fact]
        public async Task TestInternals3()
        {
            var instance = new MemoryPipeCustomReaderWriter();

            var writeTask = instance.WriteBytes(new byte[] { 4, 5, 6 }, 0, 3);

            byte[] readBuffer = new byte[3];
            var readTask = instance.ReadBytes(readBuffer, 0, readBuffer.Length);
            int readLen = await readTask;
            Assert.Equal(3, readLen);
            ComparisonUtils.CompareData(new byte[] { 4, 5, 6 }, 0, 3,
                readBuffer, 0, readLen);

            await instance.WriteBytes(new byte[10], 0, 0);

            readBuffer = new byte[4];
            readTask = instance.ReadBytes(readBuffer, 0, 4);

            writeTask = instance.WriteBytes(new byte[] { 0, 2, 4, 6, 8, 10 },
                1, 4);
            var delayTask = Task.Delay(200);
            Assert.Same(writeTask, await Task.WhenAny(writeTask, delayTask));
            
            readLen = await readTask;
            Assert.Equal(4, readLen);
            ComparisonUtils.CompareData(new byte[] { 2, 4, 6, 8 }, 0, 4,
                readBuffer, 0, readLen);

            writeTask = instance.WriteBytes(new byte[] { 0, 2, 4, 6, 8, 10 },
                1, 4);
            delayTask = Task.Delay(200);
            Assert.Same(delayTask, await Task.WhenAny(writeTask, delayTask));

            readLen = await instance.ReadBytes(readBuffer, 0, 3);
            Assert.Equal(3, readLen);
            ComparisonUtils.CompareData(new byte[] { 2, 4, 6 }, 0, 3,
                readBuffer, 0, readLen);

            readLen = await instance.ReadBytes(readBuffer, 1, 2);
            Assert.Equal(1, readLen);
            ComparisonUtils.CompareData(new byte[] { 8 }, 0, 1,
                readBuffer, 1, readLen);

            await writeTask;

            // Now test for errors
            readTask = instance.ReadBytes(readBuffer, 0, 2);

            await instance.EndWrites();
            await instance.EndWrites(new NotSupportedException()); // should have no effect

            readLen = await readTask;
            Assert.Equal(0, readLen);

            var actualEx = await Assert.ThrowsAsync<CustomIOException>(
                () => instance.WriteBytes(new byte[10], 0, 4));
            Assert.Contains("end of write", actualEx.Message);

            readLen = await instance.ReadBytes(readBuffer, 0, 3);
            Assert.Equal(0, readLen);

            readLen = await instance.ReadBytes(readBuffer, 3, 0);
            Assert.Equal(0, readLen);
        }
    }
}
