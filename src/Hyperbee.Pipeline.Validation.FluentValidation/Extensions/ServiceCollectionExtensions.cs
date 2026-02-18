using System.Reflection;
using FluentValidation;
using Hyperbee.Pipeline.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Hyperbee.Pipeline.Validation.FluentValidation;

/// <summary>
/// Provides extension methods for registering FluentValidation as the pipeline validation provider.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures FluentValidation as the pipeline validation provider.
    /// </summary>
    /// <param name="config">The pipeline validation configuration.</param>
    /// <param name="configure">Optional configuration action for specifying assemblies to scan for validators.</param>
    /// <returns>The pipeline validation configuration for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Recommended: scan for validators automatically
    /// services.AddPipelineValidation(config =>
    ///     config.UseFluentValidation(options =>
    ///         options.ScanAssembly(typeof(OrderValidator).Assembly)));
    ///
    /// // If you already register FluentValidation validators separately, omit the scanner:
    /// services.AddValidatorsFromAssemblyContaining&lt;OrderValidator&gt;();
    /// services.AddPipelineValidation(config => config.UseFluentValidation());
    /// </code>
    /// </example>
    public static IPipelineValidationConfiguration UseFluentValidation(
        this IPipelineValidationConfiguration config,
        Action<FluentValidationOptions>? configure = null )
    {
        config.Services.AddSingleton<IValidatorProvider, FluentValidatorProvider>();

        var options = new FluentValidationOptions();
        configure?.Invoke( options );

        foreach ( var assembly in options.AssembliesToScan )
        {
            config.Services.AddValidatorsFromAssembly( assembly );
        }

        return config;
    }
}

/// <summary>
/// Configuration options for FluentValidation registration.
/// </summary>
public class FluentValidationOptions
{
    /// <summary>
    /// Gets the list of assemblies to scan for FluentValidation validators.
    /// </summary>
    public List<Assembly> AssembliesToScan { get; } = [];

    /// <summary>
    /// Adds an assembly to scan for FluentValidation validators.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The options instance for method chaining.</returns>
    public FluentValidationOptions ScanAssembly( Assembly assembly )
    {
        AssembliesToScan.Add( assembly );
        return this;
    }
}
