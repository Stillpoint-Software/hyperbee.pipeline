using Hyperbee.Pipeline.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable CheckNamespace
namespace Hyperbee.Pipeline;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPipelineDefaultCacheSettings(
        this IServiceCollection services,
        DateTimeOffset? absoluteExpiration = null,
        TimeSpan? absoluteExpirationRelativeToNow = null,
        CacheItemPriority priority = CacheItemPriority.Normal,
        PostEvictionCallbackRegistration callbackRegistration = null
    )
    {
        return services.AddTransient( ( _ ) => new PipelineMemoryCacheOptions
        {
            AbsoluteExpiration = absoluteExpiration,
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow,
            Priority = priority,
            PostEvictionCallbacks = { callbackRegistration }
        } );
    }
}
