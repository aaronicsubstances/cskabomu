using Kabomu.Common;
using Kabomu.Tests.Shared;
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
            var initialReader = new DemoCustomReaderWriter(
                ByteUtils.StringToBytes(expected));
            var instance = new MemoryPipeCustomReaderWriter(initialReader);

            // act
            var t1 = IOUtils.ReadAllBytes(instance, 0, 2);
            var t2 = instance.DeferCustomDispose(() =>
                IOUtils.CopyBytes(initialReader, instance, 5));
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

            // assert that initial reader is disposed by ConcludeWriting
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                initialReader.ReadBytes(new byte[0], 0, 0));
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

            readLen = await instance.ReadBytes(readBuffer, 0, 0);
            Assert.Equal(0, readLen);

            await instance.WriteBytes(new byte[10], 0, 0);

            var writeTask = instance.WriteBytes(new byte[] { 0, 2, 4, 6, 8, 10 },
                1, 4);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => instance.WriteBytes(new byte[0], 0, 0));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => instance.WriteBytes(new byte[10], 2, 5));

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

            await instance.CustomDispose(new NotImplementedException());
            await instance.CustomDispose(new NotSupportedException()); // should have no effect

            await Assert.ThrowsAsync<NotImplementedException>(
                () => writeTask);

            await Assert.ThrowsAsync<NotImplementedException>(
                () => instance.ReadBytes(readBuffer, 0, 4));

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

            readLen = await instance.ReadBytes(readBuffer, 0, 0);
            Assert.Equal(0, readLen);

            await instance.WriteBytes(new byte[10], 0, 0);

            var writeTask = instance.WriteBytes(new byte[] { 0, 2, 4, 6, 8, 10 },
                1, 4);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => instance.WriteBytes(new byte[0], 0, 0));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => instance.WriteBytes(new byte[10], 2, 5));

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

            await instance.CustomDispose(new NotSupportedException());
            await instance.CustomDispose(); // should have no effect

            await Assert.ThrowsAsync<NotSupportedException>(
                () => readTask);

            await Assert.ThrowsAsync<NotSupportedException>(
                () => instance.ReadBytes(readBuffer, 0, 4));

            await Assert.ThrowsAsync<NotSupportedException>(
                () => instance.WriteBytes(new byte[10], 0, 4));
        }

        [Fact]
        public async Task TestInternals3()
        {
            var instance = new MemoryPipeCustomReaderWriter();
            byte[] readBuffer = new byte[3];
            var readTask = instance.ReadBytes(readBuffer, 0, readBuffer.Length);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => instance.ReadBytes(readBuffer, 0, readBuffer.Length));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => instance.ReadBytes(new byte[0], 0, 0));

            await instance.WriteBytes(new byte[] { 4, 5, 6 }, 0, 3);
            int readLen = await readTask;
            Assert.Equal(3, readLen);
            ComparisonUtils.CompareData(new byte[] { 4, 5, 6 }, 0, 3,
                readBuffer, 0, readLen);

            readLen = await instance.ReadBytes(readBuffer, 0, 0);
            Assert.Equal(0, readLen);

            await instance.WriteBytes(new byte[10], 0, 0);

            var writeTask = instance.WriteBytes(new byte[] { 0, 2, 4, 6, 8, 10 },
                1, 4);

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

            await instance.CustomDispose();
            await instance.CustomDispose(new NotSupportedException()); // should have no effect

            readLen = await readTask;
            Assert.Equal(0, readLen);

            await Assert.ThrowsAsync<EndOfWriteException>(
                () => instance.WriteBytes(new byte[10], 0, 4));

            readLen = await instance.ReadBytes(readBuffer, 0, readBuffer.Length);
            Assert.Equal(0, readLen);

            readLen = await instance.ReadBytes(readBuffer, 3, 0);
            Assert.Equal(0, readLen);
        }

        [Fact]
        public async Task TestInternals4()
        {
            var instance = new MemoryPipeCustomReaderWriter();
            byte[] readBuffer = new byte[3];
            var readTask = instance.ReadBytes(readBuffer, 0, readBuffer.Length);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => instance.ReadBytes(readBuffer, 0, readBuffer.Length));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => instance.ReadBytes(new byte[0], 0, 0));

            await instance.WriteBytes(new byte[] { 4, 5, 6 }, 0, 3);
            int readLen = await readTask;
            Assert.Equal(3, readLen);
            ComparisonUtils.CompareData(new byte[] { 4, 5, 6 }, 0, 3,
                readBuffer, 0, readLen);

            readLen = await instance.ReadBytes(readBuffer, 0, 0);
            Assert.Equal(0, readLen);

            await instance.WriteBytes(new byte[10], 0, 0);

            var writeTask = instance.WriteBytes(new byte[] { 0, 2, 4, 6, 8, 10 },
                1, 4);

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
            await instance.CustomDispose();
            await instance.CustomDispose(new NotSupportedException()); // should have no effect

            await Assert.ThrowsAsync<EndOfWriteException>(
                () => instance.WriteBytes(new byte[10], 0, 4));

            readLen = await instance.ReadBytes(readBuffer, 0, readBuffer.Length);
            Assert.Equal(0, readLen);

            readLen = await instance.ReadBytes(readBuffer, 3, 0);
            Assert.Equal(0, readLen);
        }
    }
}
