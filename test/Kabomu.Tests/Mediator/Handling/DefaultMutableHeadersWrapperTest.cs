using Kabomu.Mediator.Handling;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Handling
{
    public class DefaultMutableHeadersWrapperTest
    {
        [Fact]
        public void Test1()
        {
            var store = new Dictionary<string, IList<string>>
            {
                { "drink", null },
                { "drink2", new string[0] },
                { "drink3", new string[]{ "tea" } },
                { "drink4", new string[]{ "soup", "water" } }
            };
            var instance = new DefaultMutableHeadersWrapper(() => store, null);
            Assert.Null(instance.Get("none"));
            Assert.Null(instance.Get("drink"));
            Assert.Null(instance.Get("drink2"));
            Assert.Equal("tea", instance.Get("drink3"));
            Assert.Equal("soup", instance.Get("drink4"));

            instance.Remove("drink4");
            instance.Remove("none");
            Assert.Null(instance.Get("drink4"));

            instance.Add("m", "v");
            Assert.Equal("v", instance.Get("m"));
            instance.Add("m", "u");
            Assert.Equal("v", instance.Get("m"));
            Assert.Equal(new List<string> { "v", "u" }, store["m"]);

            instance.Set("m", "v");
            Assert.Equal("v", instance.Get("m"));
            Assert.Equal(new List<string> { "v" }, store["m"]);
            instance.Set("m", "u");
            Assert.Equal("u", instance.Get("m"));
            Assert.Equal(new List<string> { "u" }, store["m"]);
            instance.Set("m", new List<string> { "v", "u" });
            Assert.Equal("v", instance.Get("m"));
            Assert.Equal(new List<string> { "v", "u" }, store["m"]);

            Assert.Equal("tea", instance.Get("drink3"));
            instance.Clear();
            Assert.Null(instance.Get("drink3"));
            Assert.Null(instance.Get("m"));
        }

        [Fact]
        public void Test2()
        {
            var instance = new DefaultMutableHeadersWrapper(() => null, null);
            Assert.Null(instance.Get("none"));
            Assert.Null(instance.Get("drink"));
            instance.Remove("tea");
            instance.Clear();
            Assert.Null(instance.Get("drink2"));

            Assert.Throws<InvalidOperationException>(() => instance.Add("k", "v"));
            Assert.Throws<InvalidOperationException>(() => instance.Set("k", "v"));
            Assert.Throws<InvalidOperationException>(() => instance.Set("k", new List<string> { "v" }));
        }

        [Fact]
        public void Test3()
        {
            var stores = new IDictionary<string, IList<string>>[1];
            var instance = new DefaultMutableHeadersWrapper(() => stores[0], d => stores[0] = d);
            Assert.Null(instance.Get("none"));
            Assert.Null(instance.Get("drink"));
            instance.Remove("tea");
            instance.Clear();
            Assert.Null(instance.Get("drink2"));

            Assert.Null(instance.Get("m"));
            instance.Add("m", "v");
            Assert.Equal("v", instance.Get("m"));
            instance.Add("m", "u");
            Assert.Equal("v", instance.Get("m"));
            Assert.Equal(new List<string> { "v", "u" }, instance.GetAll("m"));

            instance.Set("m", "v");
            Assert.Equal("v", instance.Get("m"));
            instance.Set("m", "u");
            Assert.Equal("u", instance.Get("m"));
            instance.Set("m", new List<string> { "v", "u" });
            Assert.Equal("v", instance.Get("m"));

            // check that change of getter result work.
            stores[0] = null;

            Assert.Null(instance.Get("m"));
            instance.Add("m", "v");
            Assert.Equal("v", instance.Get("m"));
            instance.Add("m", "u");
            Assert.Equal("v", instance.Get("m"));
            Assert.Equal(new List<string> { "v", "u" }, instance.GetAll("m"));

            // check that unmodifiable array doesn't let
            // future additions fail.
            stores[0] = new Dictionary<string, IList<string>>
            {
                { "m", new string[]{ "8" } }
            };

            Assert.Equal(new List<string> { "8" }, instance.GetAll("m"));
            instance.Add("m", "v");
            Assert.Equal("8", instance.Get("m"));
            instance.Add("m", "u");
            Assert.Equal("8", instance.Get("m"));
            Assert.Equal(new List<string> { "8", "v", "u" }, instance.GetAll("m"));

            // check that set followed by add works.
            instance.Set("m2", "w");
            Assert.Equal("w", instance.Get("m2"));
            instance.Add("m2", "x");
            Assert.Equal("w", instance.Get("m2"));
            Assert.Equal(new List<string> { "w", "x" }, instance.GetAll("m2"));
        }
    }
}
