using Kabomu.Mediator.Path;

namespace Kabomu.Tests.Shared.Mediator
{
    public class TempPathTemplateGenerator : IPathTemplateGenerator
    {
        public string SpecSeen { get; set; }
        public object OptionsSeen { get; set; }
        public int ParseCallCount { get; set; }

        public IPathTemplate PathTemplateToReturn;

        public IPathTemplate Parse(string spec, object options)
        {
            ParseCallCount++;
            SpecSeen = spec;
            OptionsSeen = options;
            return PathTemplateToReturn ?? new TempPathTemplate
            {
                Spec = spec,
                Options = options
            };
        }
    }
}
