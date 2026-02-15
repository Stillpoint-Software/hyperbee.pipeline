using System.Reflection;
using Hyperbee.Pipeline.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hyperbee.Pipeline;

/// <summary>
/// Provides extension methods for registering pipeline command handlers and command classes into an <see
/// cref="IServiceCollection"/>.
/// </summary>
/// <remarks>The <c>CommandExtensions</c> class enables automatic discovery and registration of command handler
/// implementations from assemblies, simplifying integration with dependency injection in .NET applications. These
/// methods support flexible registration scenarios, including scanning a specific assembly (or the calling assembly),
/// scanning multiple assemblies, or scanning the assembly that contains a specified type, and allow customization of
/// service lifetime and filtering of types.</remarks>
public static class ServiceCollectionCommandExtensions
{
    /// <summary>
    /// Registers pipeline command handlers from the specified assembly into the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the command handlers will be added.</param>
    /// <param name="assembly">The assembly to scan for pipeline command handler implementations. If <see langword="null"/>,
    /// the calling assembly is used.</param>
    /// <param name="lifetime">The lifetime of the command services. The default is transient.</param>
    /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
    /// <param name="includeInternalTypes">Include internal command types. The default is false.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance with the pipeline command handlers registered.</returns>
    public static IServiceCollection AddPipelineCommands(
        this IServiceCollection services,
        Assembly? assembly = null,
        ServiceLifetime lifetime = ServiceLifetime.Transient,
        Func<CommandScanner.CommandScanResult, bool> filter = null,
        bool includeInternalTypes = false)
    {
        assembly ??= Assembly.GetCallingAssembly();
        return services.AddPipelineCommands([assembly], lifetime, filter, includeInternalTypes);
    }

    /// <summary>
    /// Registers command classes and their ICommand-derived interfaces from the specified assemblies.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the command services will be added.</param>
    /// <param name="assemblies">A collection of assemblies to scan for command classes.</param>
    /// <param name="lifetime">The lifetime of the command services. The default is transient.</param>
    /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
    /// <param name="includeInternalTypes">Include internal command types. The default is false.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance, allowing for method chaining.</returns>
    public static IServiceCollection AddPipelineCommands(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime lifetime = ServiceLifetime.Transient,
        Func<CommandScanner.CommandScanResult, bool> filter = null,
        bool includeInternalTypes = false)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        CommandScanner
            .FindCommandsInAssemblies(assemblies, includeInternalTypes)
            .ForEach(scanResult => services.AddScanResult(scanResult, lifetime, filter));

        return services;
    }

    /// <summary>
    /// Registers pipeline command handlers from the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="T">A type whose assembly will be scanned for command handlers.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the command handlers will be added.</param>
    /// <param name="lifetime">The lifetime of the command services. The default is transient.</param>
    /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
    /// <param name="includeInternalTypes">Include internal command types. The default is false.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddPipelineCommandsFromAssemblyContaining<T>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Transient,
        Func<CommandScanner.CommandScanResult, bool> filter = null,
        bool includeInternalTypes = false)
    {
        return services.AddPipelineCommands(typeof(T).Assembly, lifetime, filter, includeInternalTypes);
    }

    /// <summary>
    /// Registers pipeline command handlers from the assembly containing the specified type.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the command handlers will be added.</param>
    /// <param name="type">A type whose assembly will be scanned for command handlers.</param>
    /// <param name="lifetime">The lifetime of the command services. The default is transient.</param>
    /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
    /// <param name="includeInternalTypes">Include internal command types. The default is false.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddPipelineCommandsFromAssemblyContaining(
        this IServiceCollection services,
        Type type,
        ServiceLifetime lifetime = ServiceLifetime.Transient,
        Func<CommandScanner.CommandScanResult, bool> filter = null,
        bool includeInternalTypes = false)
    {
        ArgumentNullException.ThrowIfNull(type);
        return services.AddPipelineCommands(type.Assembly, lifetime, filter, includeInternalTypes);
    }

    private static void AddScanResult(
        this IServiceCollection services,
        CommandScanner.CommandScanResult scanResult,
        ServiceLifetime lifetime,
        Func<CommandScanner.CommandScanResult, bool> filter)
    {
        if (filter?.Invoke(scanResult) == false)
            return;
 
        services.TryAddEnumerable(new ServiceDescriptor(
            serviceType: scanResult.InterfaceType,
            implementationType: scanResult.CommandType,
            lifetime: lifetime));
    }
}
