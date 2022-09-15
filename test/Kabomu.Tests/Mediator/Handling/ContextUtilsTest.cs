using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Path;
using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Handling
{
    public class ContextUtilsTest
    {
        [Fact]
        public void TestGetPathTemplateGenerator()
        {
            var registry = new DefaultMutableRegistry();
            Assert.Throws<NotInRegistryException>(() => ContextUtils.GetPathTemplateGenerator(registry));
            var expected = new DefaultPathTemplateGenerator();
            registry.Add(ContextUtils.RegistryKeyPathTemplateGenerator, expected);
            var actual = ContextUtils.GetPathTemplateGenerator(registry);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void TestGetPathMatchResult()
        {
            var registry = new DefaultMutableRegistry();
            Assert.Throws<NotInRegistryException>(() => ContextUtils.GetPathMatchResult(registry));
            var expected = new DefaultPathMatchResultInternal();
            registry.Add(ContextUtils.RegistryKeyPathMatchResult, expected);
            var actual = ContextUtils.GetPathMatchResult(registry);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void TestParseUnboundRequestTarget()
        {
            var registry = new DefaultMutableRegistry();
            var generator = new TempPathTemplateGenerator();
            registry.Add(ContextUtils.RegistryKeyPathTemplateGenerator, generator);

            string spec = "/1";
            object options = null;
            var actual = (TempPathTemplate)ContextUtils.ParseUnboundRequestTarget(registry, spec, options);
            Assert.Equal(spec, actual.Spec);
            Assert.Same(options, actual.Options);

            spec = "dkk";
            options = new object();
            actual = (TempPathTemplate)ContextUtils.ParseUnboundRequestTarget(registry, spec, options);
            Assert.Equal(spec, actual.Spec);
            Assert.Same(options, actual.Options);

            spec = null;
            options = new object();
            actual = (TempPathTemplate)ContextUtils.ParseUnboundRequestTarget(registry, spec, options);
            Assert.Equal(spec, actual.Spec);
            Assert.Same(options, actual.Options);
        }

        [Fact]
        public void TestParseUnboundRequestTargetForErrors()
        {
            var registry = new DefaultMutableRegistry();
            Assert.Throws<NotInRegistryException>(() =>
                ContextUtils.ParseUnboundRequestTarget(registry, "/", null));
        }

        class TempPathTemplateGenerator : IPathTemplateGenerator
        {
            public IPathTemplate Parse(string spec, object options)
            {
                return new TempPathTemplate
                {
                    Spec = spec,
                    Options = options
                };
            }
        }

        class TempPathTemplate : IPathTemplate
        {
            public string Spec { get; set; }
            public object Options { get; set; }

            public string Interpolate(IContext context, IDictionary<string, string> pathValues, object opaqueOptionObj)
            {
                throw new NotImplementedException();
            }

            public IList<string> InterpolateAll(IContext context, IDictionary<string, string> pathValues, object opaqueOptionObj)
            {
                throw new NotImplementedException();
            }

            public IPathMatchResult Match(IContext context, string requestTarget)
            {
                throw new NotImplementedException();
            }
        }
    }
}
