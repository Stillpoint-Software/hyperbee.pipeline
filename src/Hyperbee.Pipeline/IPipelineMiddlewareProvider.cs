namespace Hyperbee.Pipeline;

/// <summary>
/// Provides hooks and wraps that can be injected into pipeline definitions.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables cross-cutting middleware concerns (logging, error mapping, etc.)
/// to be composed into pipelines without modifying individual command definitions.
/// </para>
/// <para>
/// Hooks are function middleware applied at the start of the pipeline, wrapping every step.
/// Wraps are pipeline middleware applied around the entire pipeline.
/// </para>
/// <para>
/// Register implementations in the DI container and inject into commands that need shared middleware.
/// Use <c>UseHooks</c> and <c>UseWraps</c> builder extensions for explicit control, or use
/// <c>PipelineFactory.Create</c> for a convenience wrapper that applies both automatically.
/// </para>
/// </remarks>
public interface IPipelineMiddlewareProvider
{
    /// <summary>
    /// Gets the hook middleware to apply at the start of the pipeline.
    /// </summary>
    IEnumerable<MiddlewareAsync<object, object>> Hooks { get; }

    /// <summary>
    /// Gets the wrap middleware to apply around the pipeline.
    /// </summary>
    IEnumerable<MiddlewareAsync<object, object>> Wraps { get; }
}
