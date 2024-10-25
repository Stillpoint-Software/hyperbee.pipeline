using Hyperbee.Pipeline.Extensions.Implementation;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hyperbee.Pipeline.Caching;

public static class PipelineDistributedCacheExtensions
{
    public static IPipelineBuilder<TInput, TNext> PipeDistributedCacheAsync<TInput, TOutput, TNext>(
        this IPipelineBuilder<TInput, TOutput> builder,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> nestedBuilder,
        Func<TOutput, PipelineDistributedCacheOptions, PipelineDistributedCacheOptions> optionsFunc = null )
    {
        ArgumentNullException.ThrowIfNull( nestedBuilder );

        var block = PipelineFactory.Start<TOutput>();
        var function = nestedBuilder( block ).GetPipelineFunction();

        return null; //builder.PipeDistributedCacheAsync( function.Function, optionsFunc );
    }

    public static IPipelineBuilder<TInput, TNext> PipeDistributedCacheAsync<TInput, TOutput, TNext>(
        this IPipelineBuilder<TInput, TOutput> builder,
        FunctionAsync<TOutput, TNext> next,
        Func<TOutput, PipelineDistributedCacheOptions, PipelineDistributedCacheOptions> optionsFunc = null )
    {
        // default to using input as key
        optionsFunc ??= ( output, options ) =>
        {
            options.Key = output.ToString();
            return options;
        };

        return builder.PipeAsync( async ( context, argument ) =>
        {
            var cache = context
                .ServiceProvider
                .GetService<IDistributedCache>();

            if ( cache == null )
            {
                context.Logger?.LogWarning( "Cache not configured." );
                return await next( context, argument );
            }

            var defaultCacheOption = context
                .ServiceProvider
                .GetService<IOptions<PipelineDistributedCacheOptions>>();

            var cacheOption = optionsFunc( argument,
                defaultCacheOption?.Value ?? new PipelineDistributedCacheOptions() );

            if ( cacheOption?.Key != null )
            {
                var serializer = context
                    .ServiceProvider
                    .GetService<ICacheSerializer>() ?? new JsonCacheSerializer();

                var item = await cache.GetAsync( cacheOption.Key, context.CancellationToken );
                if ( item != null )
                {
                    return await serializer.DeserializeAsync<TNext>( item, context.CancellationToken );
                }

                var result = await next( context, argument );
                var binary = await serializer.SerializeAsync( result, context.CancellationToken );
                await cache.SetAsync( cacheOption.Key, binary, cacheOption, context.CancellationToken );
                return result;
            }

            context.Logger?.LogError( "Cache entries must have a valid key." );
            context.Exception = new InvalidOperationException( "Cache entries must have a valid key." );
            context.CancelAfter();
            return default;

        } );
    }
}
