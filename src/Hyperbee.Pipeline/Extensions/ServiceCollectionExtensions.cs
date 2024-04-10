using Hyperbee.Pipeline.Context;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable CheckNamespace
namespace Hyperbee.Pipeline;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPipeline( this IServiceCollection services, bool includeAllServices = false )
    {
        return services.AddSingleton( serviceProvider =>
        {
            var factoryServices = includeAllServices ? serviceProvider : null; // use application wide service provider, or none
            return PipelineContextFactory.CreateFactory( factoryServices );
        } );
    }

    public static IServiceCollection AddPipeline( this IServiceCollection services, Action<IServiceCollection, IServiceProvider> implementationFactory )
    {
        ArgumentNullException.ThrowIfNull( implementationFactory );

        return services.AddSingleton( serviceProvider =>
        {
            var factoryServices = new ServiceCollection(); // use a specialized factory services container
            implementationFactory( factoryServices, serviceProvider );

            return PipelineContextFactory.CreateFactory( factoryServices.BuildServiceProvider() );
        } );
    }

    // Proxy a service description from the root container.
    //
    // If a service has already been defined in the root container, and we want to expose it
    // to the service factory, register a service description that requests the service
    // directly from the root container. This is especially important for singleton services.

    public static IServiceCollection ProxyService<TService>( this IServiceCollection services, IServiceProvider rootProvider )
        where TService : class
    {
        return services.AddTransient( _ => rootProvider.GetService<TService>() );
    }
}
