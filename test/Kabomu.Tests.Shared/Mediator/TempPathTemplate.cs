using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Path;

namespace Kabomu.Tests.Shared.Mediator
{
    public class TempPathTemplate : IPathTemplate
    {
        public IPathMatchResult PathMatchResultToReturn { get; set; }
        public string RequestTargetToMatch { get; set; }
        public string Spec { get; internal set; }
        public object Options { get; internal set; }

        public string Interpolate(IContext context, IDictionary<string, object> pathValues, object opaqueOptionObj)
        {
            throw new NotImplementedException();
        }

        public IList<string> InterpolateAll(IContext context, IDictionary<string, object> pathValues, object opaqueOptionObj)
        {
            throw new NotImplementedException();
        }

        public IPathMatchResult Match(IContext context, string requestTarget)
        {
            return requestTarget == RequestTargetToMatch ? PathMatchResultToReturn : null;
        }
    }
}
