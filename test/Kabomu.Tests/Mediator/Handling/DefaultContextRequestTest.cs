using Kabomu.Mediator.Handling;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Handling
{
    public class DefaultContextRequestTest
    {
        [Fact]
        public void Test1()
        {
            var rawRequest = new DefaultQuasiHttpRequest();
            var requestEnvironment = new Dictionary<string, object>();
            var instance = new DefaultContextRequest(rawRequest, requestEnvironment);
            Assert.Same(rawRequest, instance.RawRequest);
            Assert.Same(requestEnvironment, instance.Environment);
            Assert.Equal(rawRequest.Method, instance.Method);
            Assert.Equal(rawRequest.Target, instance.Target);
            Assert.Same(rawRequest.Body, instance.Body);
            Assert.NotNull(instance.Headers);
            Assert.Empty(instance.Headers.GetNames());
        }

        [Fact]
        public void Test2()
        {
            var rawRequest = new DefaultQuasiHttpRequest
            {
                Method = DefaultQuasiHttpRequest.MethodPost,
                Target = "/",
                Body = new StringBody("yes"),
                Headers = new Dictionary<string, IList<string>>
                {
                    { "lead", new string[]{ "no" } },
                    { "Flow", new string[]{ "Maybe", "no" } },
                }
            };
            var instance = new DefaultContextRequest(rawRequest, null);
            Assert.Same(rawRequest, instance.RawRequest);
            Assert.NotNull(instance.Environment);
            Assert.Equal(new Dictionary<string, object>(), instance.Environment);
            Assert.Equal(rawRequest.Method, instance.Method);
            Assert.Equal(rawRequest.Target, instance.Target);
            Assert.Same(rawRequest.Body, instance.Body);
            Assert.NotNull(instance.Headers);
            ComparisonUtils.AssertSetEqual(new List<string> { "lead", "Flow" }, instance.Headers.GetNames());
        }

        [Fact]
        public void Test3()
        {
            var rawRequest = new DefaultQuasiHttpRequest();
            var instance = new DefaultContextRequest(rawRequest, null);
            Assert.Same(rawRequest, instance.RawRequest);
            Assert.NotNull(instance.Environment);
            Assert.Equal(new Dictionary<string, object>(), instance.Environment);
            Assert.Null(instance.Method);
            Assert.Null(instance.Target);
            Assert.Null(instance.Body);
            Assert.NotNull(instance.Headers);
            Assert.Equal(new HashSet<string>(), instance.Headers.GetNames());
            CommonRegistryTestRunner.TestMutableOpsWithoutSearch(instance);
        }

        [Fact]
        public void Test4()
        {
            var rawRequest = new DefaultQuasiHttpRequest();
            var requestEnvironment = new Dictionary<string, object>
            {
                { "name", "mediator" }, { "version", 1 }, { "ssl_present", false }
            };
            var instance = new DefaultContextRequest(rawRequest, requestEnvironment);
            Assert.Same(rawRequest, instance.RawRequest);
            Assert.Same(requestEnvironment, instance.Environment);
            CommonRegistryTestRunner.TestMutableOpsWithSearch(instance);
        }
    }
}
