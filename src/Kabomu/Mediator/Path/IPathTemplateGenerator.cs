namespace Kabomu.Mediator.Path
{
    public interface IPathTemplateGenerator
    {
        IPathTemplate Parse(string part1, object part2);
    }
}