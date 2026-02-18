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

        foreach ( var entry in options.AssembliesToScan )
        {
            config.Services.AddValidatorsFromAssembly(
                entry.Assembly,
                entry.Lifetime,
                includeInternalTypes: entry.IncludeInternalTypes );
        }

        return config;
    }
}

/// <summary>
/// Configuration options for FluentValidation registration.
/// </summary>
public class FluentValidationOptions
{
    internal List<AssemblyScanEntry> AssembliesToScan { get; } = [];

    /// <summary>
    /// Scans the specified assembly for FluentValidation validators and registers them.
    /// </summary>
    /// <param name="assembly">The assembly to scan for validators.</param>
    /// <param name="lifetime">The service lifetime for registered validators. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <param name="includeInternalTypes">Whether to include internal validator types. Defaults to false.</param>
    public FluentValidationOptions ScanAssembly(
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        bool includeInternalTypes = false )
    {
        AssembliesToScan.Add( new AssemblyScanEntry( assembly, lifetime, includeInternalTypes ) );
        return this;
    }

    internal record AssemblyScanEntry(
        Assembly Assembly,
        ServiceLifetime Lifetime,
        bool IncludeInternalTypes );
}
