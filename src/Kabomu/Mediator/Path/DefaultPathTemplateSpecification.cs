using System.Collections.Generic;

namespace Kabomu.Mediator.Path
{
    public class DefaultPathTemplateSpecification
    {
        public bool CaseSensitiveMatchEnabled { get; set; }
        public bool? MatchTrailingSlash { get; set; }
        public IDictionary<string, string> DefaultValues { get; set; }
        public IList<IList<string>> SampleSets { get; set; }
        public IDictionary<string, string> ConstraintSpecs { get; set; }
    }
}