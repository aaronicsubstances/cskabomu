using Kabomu.Exceptions;
using Kabomu.ProtocolImpl;
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
    }
}
