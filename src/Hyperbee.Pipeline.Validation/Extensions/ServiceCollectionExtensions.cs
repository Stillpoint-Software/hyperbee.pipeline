using Microsoft.Extensions.DependencyInjection;

namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Provides extension methods for registering pipeline validation with the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds pipeline validation infrastructure to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">
    /// A configuration delegate for registering validation providers via <c>UseXxx</c> extension methods
    /// (e.g., <c>config.UseFluentValidation(...)</c>).
    /// </param>
    /// <returns>The service collection for method chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddPipelineValidation(config =>
    ///     config.UseFluentValidation(options =>
    ///         options.ScanAssembly(typeof(OrderValidator).Assembly)));
    /// </code>
    /// </example>
    public static IServiceCollection AddPipelineValidation(
        this IServiceCollection services,
        Action<IPipelineValidationConfiguration>? configure = null )
    {
        var config = new PipelineValidationConfiguration( services );
        configure?.Invoke( config );
        return services;
    }
}
