namespace Kabomu.Mediator.Path
{
    /// <summary>
    /// Represents builder objects which can be used to create path templates, which in turn can be used to
    /// match any request path or target, and also used for path interpolations.
    /// </summary>
    /// <remarks>
    /// This interface works with the builder design pattern to remove the cumbersome work of creating path templates directly.
    /// <para>
    /// How created path templates are built to operate are completely determined by implementations.
    /// </para>
    /// </remarks>
    public interface IPathTemplateGenerator
    {
        /// <summary>
        /// Creates a path template from some given specifications.
        /// </summary>
        /// <param name="spec">the string specification of the path template.</param>
        /// <param name="options">any options object accompanying the string specification.
        /// can also be alternative to the string specification.</param>
        /// <returns>path template instance</returns>
        IPathTemplate Parse(string spec, object options);
    }
}