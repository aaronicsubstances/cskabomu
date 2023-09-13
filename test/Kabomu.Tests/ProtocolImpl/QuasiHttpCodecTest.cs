using Kabomu.Abstractions;
using Kabomu.Exceptions;
using Kabomu.ProtocolImpl;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.ProtocolImpl
{
    public class QuasiHttpCodecTest
    {
        [Fact]
        public void TestClassConstants()
        {
            Assert.Equal("CONNECT", QuasiHttpCodec.MethodConnect);
            Assert.Equal("DELETE", QuasiHttpCodec.MethodDelete);
            Assert.Equal("GET", QuasiHttpCodec.MethodGet);
            Assert.Equal("HEAD", QuasiHttpCodec.MethodHead);
            Assert.Equal("OPTIONS", QuasiHttpCodec.MethodOptions);
            Assert.Equal("PATCH", QuasiHttpCodec.MethodPatch);
            Assert.Equal("POST", QuasiHttpCodec.MethodPost);
            Assert.Equal("PUT", QuasiHttpCodec.MethodPut);
            Assert.Equal("TRACE", QuasiHttpCodec.MethodTrace);

            Assert.Equal(200, QuasiHttpCodec.StatusCodeOk);
            Assert.Equal(500, QuasiHttpCodec.StatusCodeServerError);
            Assert.Equal(400, QuasiHttpCodec.StatusCodeClientErrorBadRequest);
            Assert.Equal(401, QuasiHttpCodec.StatusCodeClientErrorUnauthorized);
            Assert.Equal(403, QuasiHttpCodec.StatusCodeClientErrorForbidden);
            Assert.Equal(404, QuasiHttpCodec.StatusCodeClientErrorNotFound);
            Assert.Equal(405, QuasiHttpCodec.StatusCodeClientErrorMethodNotAllowed);
            Assert.Equal(413, QuasiHttpCodec.StatusCodeClientErrorPayloadTooLarge);
            Assert.Equal(414, QuasiHttpCodec.StatusCodeClientErrorURITooLong);
            Assert.Equal(415, QuasiHttpCodec.StatusCodeClientErrorUnsupportedMediaType);
            Assert.Equal(422, QuasiHttpCodec.StatusCodeClientErrorUnprocessableEntity);
            Assert.Equal(429, QuasiHttpCodec.StatusCodeClientErrorTooManyRequests);
        }

        [Fact]
        public void TestEncodeRequestHeaders1()
        {
            IQuasiHttpRequest reqHeaders = new DefaultQuasiHttpRequest();
            int? maxHeadersSize = null;
            var expected = "01\n" +
                "\"\",\"\",\"\",0\n" +
                "".PadRight(498, '\n');
            var actual = MiscUtils.BytesToString(QuasiHttpCodec.EncodeRequestHeaders(reqHeaders,
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
            var actual = MiscUtils.BytesToString(QuasiHttpCodec.EncodeRequestHeaders(reqHeaders,
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
            var actual = MiscUtils.BytesToString(QuasiHttpCodec.EncodeRequestHeaders(reqHeaders,
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
            var actual = MiscUtils.BytesToString(QuasiHttpCodec.EncodeResponseHeaders(resHeaders,
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
            var actual = MiscUtils.BytesToString(QuasiHttpCodec.EncodeResponseHeaders(resHeaders,
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
            var actual = MiscUtils.BytesToString(QuasiHttpCodec.EncodeResponseHeaders(resHeaders,
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
            byteChunks.Add(MiscUtils.StringToBytes("01\n"));
            byteChunks.Add(MiscUtils.StringToBytes("GET,/bread,HTTP/1.1,1817\n"));
            byteChunks.Add(MiscUtils.StringToBytes("90,56,\"\"\n"));
            byteChunks.Add(MiscUtils.StringToBytes("\"\",skip\n"));
            byteChunks.Add(MiscUtils.StringToBytes("9,56,uio\n"));
            byteChunks.Add(MiscUtils.StringToBytes("9\n"));
            byteChunks.Add(MiscUtils.StringToBytes("hh,\"x,y\",71\n"));
            byteChunks.Add(MiscUtils.StringToBytes("year,1999,1956,2030\n"));
            byteChunks.Add(MiscUtils.StringToBytes("hh,uio"));
            var actual = new DefaultQuasiHttpRequest();
            QuasiHttpCodec.DecodeRequestHeaders(byteChunks,
                actual);
            await ComparisonUtils.CompareRequests(expected, actual, null);
        }

        [Fact]
        public async Task TestDecodeResponseHeaders1()
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
            byteChunks.Add(MiscUtils.StringToBytes("01\n"));
            byteChunks.Add(MiscUtils.StringToBytes("369,\"\",\"\",8\n"));
            byteChunks.Add(MiscUtils.StringToBytes("really long,\"\",\"i\"\""));
            byteChunks.Add(MiscUtils.StringToBytes("".PadRight(475, ':')));
            byteChunks.Add(MiscUtils.StringToBytes("\"\n"));
            byteChunks.Add(MiscUtils.StringToBytes("".PadRight(513, '\n')));
            var actual = new DefaultQuasiHttpResponse();
            QuasiHttpCodec.DecodeResponseHeaders(byteChunks,
                actual);
            await ComparisonUtils.CompareResponses(expected, actual, null);
        }
    }
}
