using Microsoft.Extensions.DependencyInjection;

namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Provides configuration for pipeline validation, allowing validation providers to be registered
/// via <c>UseXxx</c> extension methods.
/// </summary>
public interface IPipelineValidationConfiguration
{
    /// <summary>Gets the underlying service collection.</summary>
    IServiceCollection Services { get; }
}

internal sealed class PipelineValidationConfiguration( IServiceCollection services ) : IPipelineValidationConfiguration
{
    public IServiceCollection Services { get; } = services;
}
