using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Common;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Transport
{
    public class TransportCustomReaderWriterTest
    {
        [InlineData("", "")]
        [InlineData("ab", "ab,")]
        [InlineData("abc", "ab,c,")]
        [InlineData("abcd", "ab,cd,")]
        [InlineData("abcde", "ab,cd,e,")]
        [InlineData("abcdef", "ab,cd,ef,")]
        [Theory]
        public async Task TestReading(string srcData, string expected)
        {
            // arrange
            object connection = "busuti";
            IQuasiHttpTransport transport = new DemoSimpleQuasiHttpTransport(
                connection, Encoding.UTF8.GetBytes(srcData), ",");
            var releaseConnection = false;
            var instance = new TransportCustomReaderWriter(
                transport, connection, releaseConnection);

            // act and assert
            await IOUtilsTest.TestReading(instance, null, 2,
                expected, null);
        }

        [Theory]
        [InlineData("")]
        [InlineData("d")]
        [InlineData("ds")]
        [InlineData("data")]
        [InlineData("datadriven")]
        public async Task TestWriting(string expected)
        {
            // arrange
            var reader = new DemoSimpleCustomReader(
                Encoding.UTF8.GetBytes(expected));
            object connection = "defuti";
            var transport = new DemoSimpleQuasiHttpTransport(
                connection, new byte[0], null);
            var releaseConnection = true;
            var instance = new TransportCustomReaderWriter(
                transport, connection, releaseConnection);

            // act and assert
            await IOUtilsTest.TestReading(reader, instance, 2, expected,
                _ => transport.Buffer.ToString());
        }

        [Fact]
        public async Task TestCustomDispose1()
        {
            // arrange
            object connection = "defuti";
            var transport = new DemoSimpleQuasiHttpTransport(
                connection, new byte[2], null);
            var releaseConnection = false;
            var instance = new TransportCustomReaderWriter(
                transport, connection, releaseConnection);

            // ensure reader or writer weren't disposed.
            await instance.WriteBytes(new byte[1], 0, 1);
            await instance.ReadBytes(new byte[1], 0, 1);

            await instance.CustomDispose();

            // ensure reader or writer weren't disposed.
            await instance.WriteBytes(new byte[1], 0, 1);
            await instance.ReadBytes(new byte[1], 0, 1);
        }

        [Fact]
        public async Task TestCustomDispose2()
        {
            // arrange
            object connection = "futic";
            var transport = new DemoSimpleQuasiHttpTransport(
                connection, new byte[10], null);
            var releaseConnection = true;
            var instance = new TransportCustomReaderWriter(
                transport, connection, releaseConnection);

            // ensure reader or writer weren't disposed.
            await instance.WriteBytes(new byte[1], 0, 1);
            await instance.ReadBytes(new byte[1], 0, 1);

            await instance.CustomDispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => instance.WriteBytes(new byte[1], 0, 1));
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => instance.ReadBytes(new byte[1], 0, 1));
        }
    }
}
