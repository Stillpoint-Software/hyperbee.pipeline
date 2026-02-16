using System.Reflection;
using FluentValidation;
using Hyperbee.Pipeline.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Hyperbee.Pipeline.Validation.FluentValidation;

/// <summary>
/// Provides extension methods for registering FluentValidation with the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers FluentValidation support for Hyperbee.Pipeline validation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for specifying assemblies to scan for validators.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddFluentValidation(options =>
    ///     options.ScanAssembly(typeof(OrderValidator).Assembly));
    /// </code>
    /// </example>
    public static IServiceCollection AddFluentValidation(
        this IServiceCollection services,
        Action<FluentValidationOptions>? configure = null )
    {
        services.AddSingleton<IValidatorProvider, FluentValidatorProvider>();

        // Auto-register FluentValidation validators from assembly scanning
        var options = new FluentValidationOptions();
        configure?.Invoke( options );

        foreach ( var assembly in options.AssembliesToScan )
        {
            services.AddValidatorsFromAssembly( assembly );
        }

        return services;
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
    public List<Assembly> AssembliesToScan { get; } = new();

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
