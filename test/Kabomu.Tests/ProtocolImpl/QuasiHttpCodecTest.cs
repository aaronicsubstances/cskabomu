using Kabomu.Abstractions;
using Kabomu.Exceptions;
using Kabomu.ProtocolImpl;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.ProtocolImpl
{
    public class QuasiHttpCodecTest
    {
        [Fact]
        public void TestEncodeRequestHeaders1()
        {
            IQuasiHttpRequest reqHeaders = new DefaultQuasiHttpRequest();
            int? maxHeadersSize = null;
            var expected = "01\n" +
                "\"\",\"\",\"\",0\n" +
                "".PadRight(498, '\n');
            var actual = MiscUtilsInternal.BytesToString(QuasiHttpCodec.EncodeRequestHeaders(reqHeaders,
                maxHeadersSize));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestEncodeRequestHeaders2()
        {
            IQuasiHttpRequest reqHeaders = new DefaultQuasiHttpRequest
            {
                HttpMethod = "GET",
                Target = "/bread",
                HttpVersion = "HTTP/1.1",
                ContentLength = 2018,
                Headers = new Dictionary<string, IList<string>>
                {
                    { "v90", null },
                    { "90", new string[]{ "56", null } },
                    { "", new string[]{ "skip" } },
                    { "9", new string[]{ "56", "uio" } },
                    { "hh", new string[]{ "x,y", "71", "uio" } },
                    { "year", new string[]{ "1999", "1956", "2030" } },
                }
            };
            int? maxHeadersSize = 520;
            var expected = "01\n" +
                "GET,/bread,HTTP/1.1,2018\n" +
                "90,56,\"\"\n" +
                "\"\",skip\n" +
                "9,56,uio\n" +
                "hh,\"x,y\",71,uio\n" +
                "year,1999,1956,2030\n" +
                "".PadRight(422, '\n');
            var actual = MiscUtilsInternal.BytesToString(QuasiHttpCodec.EncodeRequestHeaders(reqHeaders,
                maxHeadersSize));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestEncodeRequestHeaders3()
        {
            IQuasiHttpRequest reqHeaders = new DefaultQuasiHttpRequest
            {
                HttpMethod = "GET",
                Target = "/bread",
                HttpVersion = "HTTP/1.1" + "".PadRight(433, ' '),
                ContentLength = 1817,
                Headers = new Dictionary<string, IList<string>>
                {
                    { "90", new string[]{ "56" } },
                    { "9", new string[]{ "56", "uio" } },
                    { "", new string[]{ "x,y", "56", "uio" } },
                    { "year", new string[]{ "1999", "1956", "2030" } },
                }
            };
            int? maxHeadersSize = 1024;
            var expected = "01\n" +
                "GET,/bread," + "HTTP/1.1" + "".PadRight(433, ' ') + ",1817\n" +
                "90,56\n" +
                "9,56,uio\n" +
                "\"\",\"x,y\",56,uio\n" +
                "year,1999,1956,2030\n" +
                "".PadRight(512, '\n');
            var actual = MiscUtilsInternal.BytesToString(QuasiHttpCodec.EncodeRequestHeaders(reqHeaders,
                maxHeadersSize));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestEncodeRequestHeadersForErrors1()
        {
            var reqHeaders = new DefaultQuasiHttpRequest
            {
                HttpVersion = "".PadRight(12)
            };
            int? maxHeadersSize = 2;
            var actualEx = Assert.Throws<QuasiHttpException>(() =>
            {
                QuasiHttpCodec.EncodeRequestHeaders(reqHeaders,
                    maxHeadersSize);
            });
            Assert.Equal(QuasiHttpException.ReasonCodeMessageLengthLimitExceeded,
                actualEx.ReasonCode);
        }

        [Fact]
        public void TestEncodeRequestHeadersForErrors2()
        {
            var reqHeaders = new DefaultQuasiHttpRequest
            {
                HttpMethod = "\r\n".PadRight(12)
            };
            int? maxHeadersSize = null;
            var actualEx = Assert.Throws<QuasiHttpException>(() =>
            {
                QuasiHttpCodec.EncodeRequestHeaders(reqHeaders,
                    maxHeadersSize);
            });
            Assert.Equal(QuasiHttpException.ReasonCodeProtocolViolation,
                actualEx.ReasonCode);
        }

        [Fact]
        public void TestEncodeResponseHeaders1()
        {
            var resHeaders = new DefaultQuasiHttpResponse();
            int? maxHeadersSize = null;
            var expected = "01\n" +
                "0,\"\",\"\",0\n" +
                "".PadRight(499, '\n');
            var actual = MiscUtilsInternal.BytesToString(QuasiHttpCodec.EncodeResponseHeaders(resHeaders,
                maxHeadersSize));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestEncodeResponseHeaders2()
        {
            var resHeaders = new DefaultQuasiHttpResponse
            {
                StatusCode = 202,
                HttpStatusMessage = "done",
                HttpVersion = "HTTP/1.1" + "".PadRight(423, '2'),
                ContentLength = 12018,
                Headers = new Dictionary<string, IList<string>>
                {
                    { "9x0", null },
                    { "90", new string[]{ "56" } },
                    { "9", new string[]{ "56", "bacd" } },
                    { "", new string[]{ "x,y", "56", "uio" } },
                    { "year", new string[]{ "1999", "1956", "2030" } },
                }
            };
            int? maxHeadersSize = 512;
            var expected = "01\n" +
                "202,done," + "HTTP/1.1" + "".PadRight(423, '2') + ",12018\n" +
                "90,56\n" +
                "9,56,bacd\n" +
                "\"\",\"x,y\",56,uio\n" +
                "year,1999,1956,2030\n" +
                "".PadRight(10, '\n');
            var actual = MiscUtilsInternal.BytesToString(QuasiHttpCodec.EncodeResponseHeaders(resHeaders,
                maxHeadersSize));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestEncodeResponseHeaders3()
        {
            var resHeaders = new DefaultQuasiHttpResponse
            {
                StatusCode = 369,
                HttpStatusMessage = null,
                HttpVersion = null,
                ContentLength = 8,
                Headers = new Dictionary<string, IList<string>>
                {
                    { "nothing", new string[0] },
                    { "really long", new string[]{ null, "i\"" + "".PadRight(475, ':') } }
                }
            };
            int? maxHeadersSize = 2_000;
            var expected = "01\n" +
                "369,\"\",\"\",8\n" +
                "really long,\"\"," + "\"i\"\"" + "".PadRight(475, ':') + "\"\n" +
                "".PadRight(513, '\n');
            var actual = MiscUtilsInternal.BytesToString(QuasiHttpCodec.EncodeResponseHeaders(resHeaders,
                maxHeadersSize));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestEncodeResponseHeadersForErrors1()
        {
            var resHeaders = new DefaultQuasiHttpResponse
            {
                HttpStatusMessage = "".PadRight(475)
            };
            int? maxHeadersSize = 20;
            var actualEx = Assert.Throws<QuasiHttpException>(() =>
            {
                QuasiHttpCodec.EncodeResponseHeaders(resHeaders,
                    maxHeadersSize);
            });
            Assert.Equal(QuasiHttpException.ReasonCodeMessageLengthLimitExceeded,
                actualEx.ReasonCode);
        }

        [Fact]
        public void TestEncodeResponseHeadersForErrors2()
        {
            var resHeaders = new DefaultQuasiHttpResponse
            {
                HttpStatusMessage = "\n".PadRight(45)
            };
            int? maxHeadersSize = 2_000;
            var actualEx = Assert.Throws<QuasiHttpException>(() =>
            {
                QuasiHttpCodec.EncodeResponseHeaders(resHeaders,
                    maxHeadersSize);
            });
            Assert.Equal(QuasiHttpException.ReasonCodeProtocolViolation,
                actualEx.ReasonCode);
        }

        [Fact]
        public async Task TestDecodeRequestHeaders1()
        {
            var expected = new DefaultQuasiHttpRequest
            {
                HttpMethod = "",
                Target = "",
                HttpVersion = "",
                ContentLength = 0,
                Headers = new Dictionary<string, IList<string>>()
            };
            var byteChunk = MiscUtilsInternal.StringToBytes("01\n" +
                "\"\",\"\",\"\",0\n" +
                "".PadRight(498, '\n'));
            var actual = new DefaultQuasiHttpRequest();
            QuasiHttpCodec.DecodeRequestHeaders(byteChunk, 0, byteChunk.Length,
                actual);
            await ComparisonUtils.CompareRequests(expected, actual, null);
        }

        [Fact]
        public async Task TestDecodeRequestHeaders2()
        {
            var expected = new DefaultQuasiHttpRequest
            {
                HttpMethod = "GET",
                Target = "/bread",
                HttpVersion = "HTTP/1.1",
                ContentLength = 1817,
                Headers = new Dictionary<string, IList<string>>
                {
                    { "90", new string[]{ "56", "" } },
                    { "", new string[]{ "skip" } },
                    { "9", new string[]{ "56", "uio" } },
                    { "hh", new string[]{ "x,y", "71", "uio" } },
                    { "year", new string[]{ "1999", "1956", "2030" } },
                }
            };
            var byteChunks = new List<byte[]>();
            byteChunks.Add(new byte[90]);
            byteChunks.Add(MiscUtilsInternal.StringToBytes("01\n"));
            byteChunks.Add(MiscUtilsInternal.StringToBytes("GET,/bread,HTTP/1.1,1817\n"));
            byteChunks.Add(MiscUtilsInternal.StringToBytes("90,56,\"\"\n"));
            byteChunks.Add(MiscUtilsInternal.StringToBytes("\"\",skip\n"));
            byteChunks.Add(MiscUtilsInternal.StringToBytes("9,56,uio\n"));
            byteChunks.Add(MiscUtilsInternal.StringToBytes("9\n"));
            byteChunks.Add(MiscUtilsInternal.StringToBytes("hh,\"x,y\",71\n"));
            byteChunks.Add(MiscUtilsInternal.StringToBytes("year,1999,1956,2030\n"));
            byteChunks.Add(MiscUtilsInternal.StringToBytes("hh,uio"));
            byteChunks.Add(new byte[100]);
            var actual = new DefaultQuasiHttpRequest();
            var combined = MiscUtilsInternal.ConcatBuffers(byteChunks);
            QuasiHttpCodec.DecodeRequestHeaders(combined, 90,
                combined.Length - 190, actual);
            await ComparisonUtils.CompareRequests(expected, actual, null);
        }

        [Theory]
        [InlineData("", "invalid quasi http request headers")]
        [InlineData("\n\n", "invalid quasi http request headers")]
        [InlineData("x\nGET,/d,1.1,9",
            "invalid quasi http request headers")]
        [InlineData("01", "invalid quasi http request headers")]
        [InlineData("01\",m\"p]", "invalid quasi http request headers")]
        [InlineData("01\n2", "invalid quasi http request line")]
        [InlineData("01\nGET,/,1.1,x0",
            "invalid quasi http request content length")]
        public void TestDecodeRequestHeadersForErrors(string data,
            string errorMsg)
        {
            var request = new DefaultQuasiHttpRequest();
            var byteChunk = MiscUtilsInternal.StringToBytes(data);
            var actualEx = Assert.Throws<QuasiHttpException>(() =>
            {
                QuasiHttpCodec.DecodeRequestHeaders(byteChunk, 0,
                    byteChunk.Length, request);
            });
            Assert.Equal(errorMsg, actualEx.Message);
        }

        [Fact]
        public async Task TestDecodeResponseHeaders1()
        {
            var expected = new DefaultQuasiHttpResponse
            {
                StatusCode = 0,
                HttpStatusMessage = "",
                HttpVersion = "",
                ContentLength = 0,
                Headers = new Dictionary<string, IList<string>>()
            };
            var byteChunk = MiscUtilsInternal.StringToBytes("01\n" +
                "0,\"\",\"\",0\n");
            var actual = new DefaultQuasiHttpResponse();
            QuasiHttpCodec.DecodeResponseHeaders(byteChunk, 0,
                byteChunk.Length, actual);
            await ComparisonUtils.CompareResponses(expected, actual, null);
        }

        [Fact]
        public async Task TestDecodeResponseHeaders2()
        {
            var expected = new DefaultQuasiHttpResponse
            {
                StatusCode = 369,
                HttpStatusMessage = "",
                HttpVersion = "",
                ContentLength = 8,
                Headers = new Dictionary<string, IList<string>>
                {
                    { "really long", new string[]{ "", "i\"" + "".PadRight(475, ':') } }
                }
            };
            var byteChunks = new List<byte[]>();
            byteChunks.Add(new byte[20]);
            byteChunks.Add(MiscUtilsInternal.StringToBytes("01\n"));
            byteChunks.Add(MiscUtilsInternal.StringToBytes("369,\"\",\"\",8\n"));
            byteChunks.Add(MiscUtilsInternal.StringToBytes("really long,\"\",\"i\"\""));
            byteChunks.Add(MiscUtilsInternal.StringToBytes("".PadRight(475, ':')));
            byteChunks.Add(MiscUtilsInternal.StringToBytes("\"\n"));
            byteChunks.Add(MiscUtilsInternal.StringToBytes("".PadRight(513, '\n')));
            byteChunks.Add(new byte[10]);
            var actual = new DefaultQuasiHttpResponse();
            var combined = MiscUtilsInternal.ConcatBuffers(byteChunks);
            QuasiHttpCodec.DecodeResponseHeaders(combined, 20,
                combined.Length - 30, actual);
            await ComparisonUtils.CompareResponses(expected, actual, null);
        }

        [Theory]
        [InlineData("", "invalid quasi http response headers")]
        [InlineData("\n\n", "invalid quasi http response headers")]
        [InlineData("x\n200,/d,1.1,9",
            "invalid quasi http response headers")]
        [InlineData("01", "invalid quasi http response headers")]
        [InlineData("01\",n\"m",
            "invalid quasi http response headers")]
        [InlineData("01\n2", "invalid quasi http status line")]
        [InlineData("01\n,ok,1.1,0",
            "invalid quasi http response status code")]
        [InlineData("01\n98,ok,1.1,x0",
            "invalid quasi http response content length")]
        public void TestDecodeResponseHeadersForErrors(string data,
            string errorMsg)
        {
            var response = new DefaultQuasiHttpResponse();
            var byteChunk = MiscUtilsInternal.StringToBytes(data);
            var actualEx = Assert.Throws<QuasiHttpException>(() =>
            {
                QuasiHttpCodec.DecodeResponseHeaders(
                    byteChunk, 0, byteChunk.Length,
                    response);
            });
            Assert.Equal(errorMsg, actualEx.Message);
        }
    }
}
