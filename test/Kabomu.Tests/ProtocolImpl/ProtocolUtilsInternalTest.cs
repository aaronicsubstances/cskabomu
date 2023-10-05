using Kabomu.Exceptions;
using Kabomu.ProtocolImpl;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Kabomu.Tests.ProtocolImpl
{
    public class ProtocolUtilsInternalTest
    {
        [Theory]
        [MemberData(nameof(CreateTestGetEnvVarAsBooleanData))]
        public void TestGetEnvVarAsBoolean(IDictionary<string, object> environment,
            string key, bool? expected)
        {
            var actual = ProtocolUtilsInternal.GetEnvVarAsBoolean(environment,
                key);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestGetEnvVarAsBooleanData()
        {
            var testData = new List<object[]>();

            var environment = new Dictionary<string, object>
            {
                { "d", "de" },
                { "2", false }
            };
            string key = "2";
            testData.Add(new object[] { environment, key, false });

            environment = null;
            key = "k1";
            testData.Add(new object[] { environment, key, null });

            environment = new Dictionary<string, object>
            {
                { "d2", "TRUE" }, { "e", "ghana" }
            };
            key = "f";
            testData.Add(new object[] { environment, key, null });

            environment = new Dictionary<string, object>
            {
                { "ty2", "TRUE" }, { "c", new object() }
            };
            key = "ty2";
            testData.Add(new object[] { environment, key, true });

            environment = new Dictionary<string, object>
            {
                { "d2", true }, { "e", "ghana" }
            };
            key = "d2";
            testData.Add(new object[] { environment, key, true });

            environment = new Dictionary<string, object>
            {
                { "d", "TRue" }, { "e", "ghana" }
            };
            key = "d";
            testData.Add(new object[] { environment, key, true });

            environment = new Dictionary<string, object>
            {
                { "d", "FALSE" }, { "e", "ghana" }
            };
            key = "d";
            testData.Add(new object[] { environment, key, false });

            environment = new Dictionary<string, object>
            {
                { "d", "45" }, { "e", "ghana" }, { "ert", "False" }
            };
            key = "ert";
            testData.Add(new object[] { environment, key, false });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestGetEnvVarAsBooleanForErrorsData))]
        public void TestGetEnvVarAsBooleanForErrors(IDictionary<string, object> environment,
            string key)
        {
            Assert.ThrowsAny<Exception>(() =>
                ProtocolUtilsInternal.GetEnvVarAsBoolean(environment, key));
        }

        public static List<object[]> CreateTestGetEnvVarAsBooleanForErrorsData()
        {
            var testData = new List<object[]>();

            var environment = new Dictionary<string, object>
            {
                { "d", "de" },
                { "2", false }
            };
            string key = "d";
            testData.Add(new object[] { environment, key });

            environment = new Dictionary<string, object>
            {
                { "c", "" }
            };
            key = "c";
            testData.Add(new object[] { environment, key });

            environment = new Dictionary<string, object>
            {
                { "d2", "TRUE" }, { "e", new List<string>() }
            };
            key = "e";
            testData.Add(new object[] { environment, key });

            environment = new Dictionary<string, object>
            {
                { "k1", 1 }
            };
            key = "k1";
            testData.Add(new object[] { environment, key });

            environment = new Dictionary<string, object>
            {
                { "k1", 0 }
            };
            key = "k1";
            testData.Add(new object[] { environment, key });

            return testData;
        }

        [Fact]
        public async Task TestWrapTimeoutTask1()
        {
            await ProtocolUtilsInternal.WrapTimeoutTask(null, "");
        }

        [Fact]
        public async Task TestWrapTimeoutTask2()
        {
            var task = Task.FromResult(false);
            await ProtocolUtilsInternal.WrapTimeoutTask(task, "");
        }

        [Fact]
        public async Task TestWrapTimeoutTask3()
        {
            var task = Task.FromResult(true);
            var actualEx = await Assert.ThrowsAsync<QuasiHttpException>(() =>
            {
                return ProtocolUtilsInternal.WrapTimeoutTask(task, "te");
            });
            Assert.Equal("te", actualEx.Message);
            Assert.Equal(QuasiHttpException.ReasonCodeTimeout, actualEx.ReasonCode);
        }

        [Fact]
        public async Task TestWrapTimeoutTask4()
        {
            var task = Task.FromResult(true);
            var actualEx = await Assert.ThrowsAsync<QuasiHttpException>(() =>
            {
                return ProtocolUtilsInternal.WrapTimeoutTask(task, "recv");
            });
            Assert.Equal("recv", actualEx.Message);
            Assert.Equal(QuasiHttpException.ReasonCodeTimeout, actualEx.ReasonCode);
        }

        [Fact]
        public async Task TestWrapTimeoutTask5()
        {
            var task = Task.FromException<bool>(new ArgumentException("th"));
            var actualEx = await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return ProtocolUtilsInternal.WrapTimeoutTask(task, "te");
            });
            Assert.Equal("th", actualEx.Message);
        }

        [Fact]
        public async Task TestWrapTimeoutTask6()
        {
            var task = Task.FromException<bool>(
                new KabomuIOException("2gh"));
            var actualEx = await Assert.ThrowsAsync<KabomuIOException>(() =>
            {
                return ProtocolUtilsInternal.WrapTimeoutTask(task, "tfe");
            });
            Assert.Equal("2gh", actualEx.Message);
        }

        [Theory]
        [MemberData(nameof(CreateTestContainsOnlyPrintableAsciiCharsData))]
        public void TestContainsOnlyPrintableAsciiChars(string v,
            bool allowSpace, bool expected)
        {
            var actual = ProtocolUtilsInternal.ContainsOnlyPrintableAsciiChars(
                v, allowSpace);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestContainsOnlyPrintableAsciiCharsData()
        {
            return new List<object[]>
            {
                new object[]{ "x.n", false, true },
                new object[]{ "x\n", false, false },
                new object[]{ "yd\u00c7ea", true, false },
                new object[]{ "x m", true, true },
                new object[]{ "x m", false, false },
                new object[]{ "x-yio", true, true },
                new object[]{ "x-yio", false, true },
                new object[]{ "x", true, true },
                new object[]{ "x", false, true },
                new object[]{ @" !@#$%^&*()_+=-{}[]|\:;""'?/>.<,'",
                    false, false },
                new object[]{ @"!@#$%^&*()_+=-{}[]|\:;""'?/>.<,'",
                    false, true },
                new object[]{ @" !@#$%^&*()_+=-{}[]|\:;""'?/>.<,'",
                    true, true },
            };
        }

        [Theory]
        [MemberData(nameof(CreateContainsOnlyHeaderNameCharsData))]
        public void TestContainsOnlyHeaderNameChars(string v, bool expected)
        {
            var actual = ProtocolUtilsInternal.ContainsOnlyHeaderNameChars(v);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateContainsOnlyHeaderNameCharsData()
        {
            return new List<object[]>
            {
                new object[]{ "x\n", false },
                new object[]{ "yd\u00c7ea", false },
                new object[]{ "x m", false },
                new object[]{ "xmX123abcD", true },
                new object[]{ "xm", true },
                new object[]{ "x-yio", true },
                new object[]{ "x:yio", false },
                new object[]{ "123", true },
                new object[]{ "x", true },
            };
        }

        [Fact]
        public void TestValidateHttpHeaderSection1()
        {
            var csv = new List<IList<string>>
            {
                new string[]{ "GET", "/", "HTTP/1.0", "24" }
            };
            ProtocolUtilsInternal.ValidateHttpHeaderSection(false,
                csv);
        }

        [Fact]
        public void TestValidateHttpHeaderSection2()
        {
            var csv = new List<IList<string>>
            {
                new string[]{ "HTTP/1.0", "204", "No Content", "-10" },
                new string[]{ "Content-Type", "application/json; charset=UTF8" },
                new string[]{ "Transfer-Encoding", "chunked" },
                new string[]{ "Date", "Tue, 15 Nov 1994 08:12:31 GMT" },
                new string[]{ "Authorization", "Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==" },
                new string[]{ "User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:12.0) Gecko/20100101 Firefox/12.0" }
            };
            ProtocolUtilsInternal.ValidateHttpHeaderSection(true,
                csv);
        }

        [Theory]
        [MemberData(nameof(CreateTestValidateHttpHeaderSectionForErrorsData))]
        public void TestValidateHttpHeaderSectionForErrors(bool isResponse,
            IList<IList<string>> csv,
            string expectedErrorMessage)
        {
            var actualEx = Assert.Throws<QuasiHttpException>(() =>
            {
                ProtocolUtilsInternal.ValidateHttpHeaderSection(isResponse, csv);
            });
            Assert.Equal(QuasiHttpException.ReasonCodeProtocolViolation, actualEx.ReasonCode);
            Assert.Contains(expectedErrorMessage, actualEx.Message);
        }

        public static List<object[]> CreateTestValidateHttpHeaderSectionForErrorsData()
        {
            var testData = new List<object[]>();

            bool isResponse = true;
            var csv = new List<IList<string>>
            {
                new string[]{ "HTTP/1 0", "200", "OK", "-10" }
            };
            string expectedErrorMessage = "quasi http status line field contains spaces";
            testData.Add(new object[] { isResponse, csv, expectedErrorMessage });

            isResponse = false;
            csv = new List<IList<string>>
            {
                new string[]{ "HTTP/1.0", "20 4", "OK", "-10" }
            };
            expectedErrorMessage = "quasi http request line field contains spaces";
            testData.Add(new object[] { isResponse, csv, expectedErrorMessage });

            isResponse = true;
            csv = new List<IList<string>>
            {
                new string[]{ "HTTP/1.0", "200", "OK", "-1 0" }
            };
            expectedErrorMessage = "quasi http status line field contains spaces";
            testData.Add(new object[] { isResponse, csv, expectedErrorMessage });

            isResponse = true;
            csv = new List<IList<string>>
            {
                new string[]{ "HTTP/1.0", "200", "OK", "0" },
                new string[]{ "Content:Type", "application/json; charset=UTF8" },
            };
            expectedErrorMessage = "quasi http header name contains characters other than hyphen";
            testData.Add(new object[] { isResponse, csv, expectedErrorMessage });

            isResponse = false;
            csv = new List<IList<string>>
            {
                new string[]{ "HTTP/1.0", "200", "OK", "51" },
                new string[]{ "Content-Type", "application/json; charset=UTF8\n" },
            };
            expectedErrorMessage = "quasi http header value contains newlines";
            testData.Add(new object[] { isResponse, csv, expectedErrorMessage });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestEncodeQuasiHttpHeadersData))]
        public void TestEncodeQuasiHttpHeaders(bool isResponse,
            IList<string> reqOrStatusLine,
            IDictionary<string, IList<string>> remainingHeaders,
            string expected)
        {
            byte[] actual = ProtocolUtilsInternal.EncodeQuasiHttpHeaders(
                isResponse, reqOrStatusLine, remainingHeaders);
            Assert.Equal(Encoding.UTF8.GetBytes(expected), actual);
        }

        public static List<object[]> CreateTestEncodeQuasiHttpHeadersData()
        {
            var testData = new List<object[]>();

            bool isResponse = false;
            IList<string> reqOrStatusLine = new string[]
            {
                "GET",
                "/home/index?q=results",
                "HTTP/1.1",
                "-1"
            };
            var remainingHeaders = new Dictionary<string, IList<string>>
            {
                { "Content-Type", new string[]{ "text/plain" } }
            };
            string expected = "GET,/home/index?q=results,HTTP/1.1,-1\n" +
                "Content-Type,text/plain\n";
            testData.Add(new object[] { isResponse, reqOrStatusLine,
                remainingHeaders, expected });

            isResponse = true;
            reqOrStatusLine = new string[]
            {
                "HTTP/1.1",
                "200",
                "OK",
                "12"
            };
            remainingHeaders = new Dictionary<string, IList<string>>
            {
                { "Content-Type", new string[]{ "text/plain", "text/csv" } },
                { "Accept", new string[]{ "text/html" } },
                { "Accept-Charset", new string[]{ "utf-8" } }
            };
            expected = "HTTP/1.1,200,OK,12\n" +
                "Content-Type,text/plain,text/csv\n" +
                "Accept,text/html\n" +
                "Accept-Charset,utf-8\n";
            testData.Add(new object[] { isResponse, reqOrStatusLine,
                remainingHeaders, expected });

            isResponse = false;
            reqOrStatusLine = new string[]
            {
                null,
                null,
                null,
                "0"
            };
            remainingHeaders = null;
            expected = "\"\",\"\",\"\",0\n";
            testData.Add(new object[] { isResponse, reqOrStatusLine,
                remainingHeaders, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestEncodeQuasiHttpHeadersForErrorsData))]
        public void TestEncodeQuasiHttpHeadersForErrors(bool isResponse,
            IList<string> reqOrStatusLine,
            IDictionary<string, IList<string>> remainingHeaders,
            string expectedErrorMessage)
        {
            var actualEx = Assert.Throws<QuasiHttpException>(() =>
            {
                ProtocolUtilsInternal.EncodeQuasiHttpHeaders(
                    isResponse, reqOrStatusLine, remainingHeaders);
            });
            Assert.Equal(QuasiHttpException.ReasonCodeProtocolViolation,
                actualEx.ReasonCode);
            Assert.Contains(expectedErrorMessage, actualEx.Message);
        }

        public static List<object[]> CreateTestEncodeQuasiHttpHeadersForErrorsData()
        {
            var testData = new List<object[]>();

            bool isResponse = false;
            IList<string> reqOrStatusLine = new string[]
            {
                "GET",
                "/home/index?q=results",
                "HTTP/1.1",
                "-1"
            };
            var remainingHeaders = new Dictionary<string, IList<string>>
            {
                { "", new string[]{ "text/plain" } }
            };
            string expected = "quasi http header name cannot be empty";
            testData.Add(new object[] { isResponse, reqOrStatusLine,
                remainingHeaders, expected });

            isResponse = true;
            reqOrStatusLine = new string[]
            {
                "HTTP/1.1",
                "400",
                "Bad Request",
                "12"
            };
            remainingHeaders = new Dictionary<string, IList<string>>
            {
                { "Content-Type", new string[]{ "", "text/csv" } },
            };
            expected = "quasi http header value cannot be empty";
            testData.Add(new object[] { isResponse, reqOrStatusLine,
                remainingHeaders, expected });

            isResponse = false;
            reqOrStatusLine = new string[]
            {
                "GET or POST",
                null,
                null,
                "0"
            };
            remainingHeaders = null;
            expected = "quasi http request line field contains spaces";
            testData.Add(new object[] { isResponse, reqOrStatusLine,
                remainingHeaders, expected });

            isResponse = false;
            reqOrStatusLine = new string[]
            {
                "GET",
                null,
                null,
                "0 or 1"
            };
            remainingHeaders = null;
            expected = "quasi http request line field contains spaces";
            testData.Add(new object[] { isResponse, reqOrStatusLine,
                remainingHeaders, expected });

            isResponse = true;
            reqOrStatusLine = new string[]
            {
                "HTTP 1.1",
                "200",
                "OK",
                "0"
            };
            remainingHeaders = null;
            expected = "quasi http status line field contains spaces";
            testData.Add(new object[] { isResponse, reqOrStatusLine,
                remainingHeaders, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestDecodeQuasiHttpHeadersData))]
        public void TestDecodeQuasiHttpHeaders(bool isResponse,
            byte[] data, int offset, int length,
            IDictionary<string, IList<string>> expectedHeaders,
            IList<string> expectedReqOrStatusLine)
        {
            var headersReceiver = new Dictionary<string, IList<string>>();
            var actualReqOrStatusLine = ProtocolUtilsInternal.DecodeQuasiHttpHeaders(
                isResponse, data, offset, length,
                headersReceiver);
            Assert.Equal(expectedReqOrStatusLine, actualReqOrStatusLine);
            ComparisonUtils.CompareHeaders(expectedHeaders, headersReceiver);
        }

        public static List<object[]> CreateTestDecodeQuasiHttpHeadersData()
        {
            var testData = new List<object[]>();

            bool isResponse = false;
            byte[] data = Encoding.UTF8.GetBytes(
                "GET,/home/index?q=results,HTTP/1.1,-1\n" +
                "Content-Type,text/plain\n");
            int offset = 0;
            int length = data.Length;
            var expectedHeaders = new Dictionary<string, IList<string>>
            {
                { "content-type", new string[]{ "text/plain" } }
            };
            var expectedReqOrStatusLine = new string[]
            {
                "GET",
                "/home/index?q=results",
                "HTTP/1.1",
                "-1"
            };
            testData.Add(new object[] { isResponse, data, offset,
                length, expectedHeaders, expectedReqOrStatusLine });

            isResponse = true;
            data = Encoding.UTF8.GetBytes("HTTP/1.1,200,OK,12\n" +
                "Content-Type,text/plain,text/csv\n" +
                "content-type,application/json\n" +
                "\r\n" +
                "ignored\n" +
                "Accept,text/html\n" +
                "Accept-Charset,utf-8\n\"");
            offset = 0;
            length = data.Length - 1;
            expectedHeaders = new Dictionary<string, IList<string>>
            {
                { "content-type", new string[]{
                    "text/plain", "text/csv", "application/json" } },
                { "accept", new string[]{ "text/html" } },
                { "accept-charset", new string[]{ "utf-8" } }
            };
            expectedReqOrStatusLine = new string[]
            {
                "HTTP/1.1",
                "200",
                "OK",
                "12"
            };
            testData.Add(new object[] { isResponse, data, offset,
                length, expectedHeaders, expectedReqOrStatusLine });

            isResponse = false;
            data = Encoding.UTF8.GetBytes("\"\",\"\",\"\",0\n");
            offset = 0;
            length = data.Length;
            expectedHeaders = new Dictionary<string, IList<string>>();
            expectedReqOrStatusLine = new string[]
            {
                "",
                "",
                "",
                "0"
            };
            testData.Add(new object[] { isResponse, data, offset,
                length, expectedHeaders, expectedReqOrStatusLine });

            isResponse = true;
            data = Encoding.UTF8.GetBytes(
                "k\"GET,/home/index?q=results,HTTP/1.1,-1\n" +
                "Content-Type,text/plain\nk2\"");
            offset = 2;
            length = data.Length - 5;
            expectedHeaders = new Dictionary<string, IList<string>>
            {
                { "content-type", new string[]{ "text/plain" } }
            };
            expectedReqOrStatusLine = new string[]
            {
                "GET",
                "/home/index?q=results",
                "HTTP/1.1",
                "-1"
            };
            testData.Add(new object[] { isResponse, data, offset,
                length, expectedHeaders, expectedReqOrStatusLine });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestDecodeQuasiHttpHeadersForErrorsData))]
        public void TestDecodeQuasiHttpHeadersForErrors(bool isResponse,
            byte[] data, int offset, int length,
            string expectedErrorMessage)
        {
            var actualEx = Assert.Throws<QuasiHttpException>(() =>
            {
                ProtocolUtilsInternal.DecodeQuasiHttpHeaders(
                    isResponse, data, offset, length,
                    new Dictionary<string, IList<string>>());
            });
            Assert.Equal(QuasiHttpException.ReasonCodeProtocolViolation,
                actualEx.ReasonCode);
            Assert.Contains(expectedErrorMessage, actualEx.Message);
        }

        public static List<object[]> CreateTestDecodeQuasiHttpHeadersForErrorsData()
        {
            var testData = new List<object[]>();

            bool isResponse = false;
            byte[] data = Encoding.UTF8.GetBytes(
                "\"k\n,lopp");
            int offset = 0;
            int length = data.Length;
            var expectedErrorMessage = "invalid quasi http headers";
            testData.Add(new object[] { isResponse, data, offset,
                length, expectedErrorMessage });

            isResponse = false;
            data = Encoding.UTF8.GetBytes("");
            offset = 0;
            length = data.Length;
            expectedErrorMessage = "invalid quasi http headers";
            testData.Add(new object[] { isResponse, data, offset,
                length, expectedErrorMessage });

            isResponse = true;
            data = Encoding.UTF8.GetBytes("HTTP/1.1,200");
            offset = 0;
            length = data.Length;
            expectedErrorMessage = "invalid quasi http status line";
            testData.Add(new object[] { isResponse, data, offset,
                length, expectedErrorMessage });

            isResponse = false;
            data = Encoding.UTF8.GetBytes("GET,HTTP/1.1,");
            offset = 0;
            length = data.Length;
            expectedErrorMessage = "invalid quasi http request line";
            testData.Add(new object[] { isResponse, data, offset,
                length, expectedErrorMessage });

            return testData;
        }
    }
}
