using Kabomu.Mediator.Handling;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared.Common;
using Kabomu.Tests.Shared.Mediator;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Handling
{
    public class DefaultContextRequestInternalTest
    {
        [Fact]
        public void Test1()
        {
            var rawRequest = new DefaultQuasiHttpRequest();
            var instance = new DefaultContextRequestInternal(rawRequest);
            Assert.Same(rawRequest, instance.RawRequest);
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
                Method = QuasiHttpUtils.MethodPost,
                Target = "/",
                Body = new StringBody("yes"),
                Headers = new Dictionary<string, IList<string>>
                {
                    { "lead", new string[]{ "no" } },
                    { "Flow", new string[]{ "Maybe", "no" } },
                }
            };
            var instance = new DefaultContextRequestInternal(rawRequest);
            Assert.Same(rawRequest, instance.RawRequest);
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
            var instance = new DefaultContextRequestInternal(rawRequest);
            Assert.Same(rawRequest, instance.RawRequest);
            Assert.Null(instance.Method);
            Assert.Null(instance.Target);
            Assert.Null(instance.Body);
            Assert.NotNull(instance.Headers);
            Assert.Equal(new HashSet<string>(), instance.Headers.GetNames());
            CommonRegistryTestRunner.TestMutableOps(instance);
        }
    }
}
