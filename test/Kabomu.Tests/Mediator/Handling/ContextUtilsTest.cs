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

            string part1 = "/1";
            object part2 = null;
            var actual = (TempPathTemplate)ContextUtils.ParseUnboundRequestTarget(registry, part1, part2);
            Assert.Equal(part1, actual.Part1);
            Assert.Same(part2, actual.Part2);

            part1 = "dkk";
            part2 = new object();
            actual = (TempPathTemplate)ContextUtils.ParseUnboundRequestTarget(registry, part1, part2);
            Assert.Equal(part1, actual.Part1);
            Assert.Same(part2, actual.Part2);

            part1 = null;
            part2 = new object();
            actual = (TempPathTemplate)ContextUtils.ParseUnboundRequestTarget(registry, part1, part2);
            Assert.Equal(part1, actual.Part1);
            Assert.Same(part2, actual.Part2);
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
            public IPathTemplate Parse(string part1, object part2)
            {
                return new TempPathTemplate
                {
                    Part1 = part1,
                    Part2 = part2
                };
            }
        }

        class TempPathTemplate : IPathTemplate
        {
            public string Part1 { get; set; }
            public object Part2 { get; set; }

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
