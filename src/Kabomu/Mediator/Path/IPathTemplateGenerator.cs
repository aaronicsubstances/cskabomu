namespace Kabomu.Mediator.Path
{
    public interface IPathTemplateGenerator
    {
        IPathTemplate Parse(string pathSpec);
    }
}