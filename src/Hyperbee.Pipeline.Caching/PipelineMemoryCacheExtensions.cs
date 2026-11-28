using Hyperbee.Pipeline.Extensions.Implementation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hyperbee.Pipeline.Caching;

public static class PipelineMemoryCacheExtensions
{
    public static IPipelineBuilder<TStart, TNext> PipeCache<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> builder,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> nestedBuilder,
        Func<TOutput, PipelineMemoryCacheOptions, PipelineMemoryCacheOptions> optionsFunc = null )
    {
        ArgumentNullException.ThrowIfNull( nestedBuilder );

        var block = PipelineFactory.Start<TOutput>();
        var function = nestedBuilder( block ).GetPipelineFunction();

        return builder.PipeCacheAsync( function.Function, optionsFunc );
    }

    public static IPipelineBuilder<TStart, TNext> PipeCacheAsync<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> builder,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> nestedBuilder,
        Func<TOutput, PipelineMemoryCacheOptions, PipelineMemoryCacheOptions> optionsFunc = null )
    {
        ArgumentNullException.ThrowIfNull( nestedBuilder );

        var block = PipelineFactory.Start<TOutput>();
        var function = nestedBuilder( block ).GetPipelineFunction();

        return builder.PipeCacheAsync( function.Function, optionsFunc );
    }

    public static IPipelineBuilder<TStart, TNext> PipeCache<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> builder,
        Function<TOutput, TNext> next,
        Func<TOutput, PipelineMemoryCacheOptions, PipelineMemoryCacheOptions> optionsFunc = null )
    {

        // default to using input as key
        optionsFunc ??= ( output, options ) =>
        {
            options.Key = output;
            return options;
        };


        return builder.Pipe( ( context, argument ) =>
        {
            var cache = context
                .ServiceProvider
                .GetService<IMemoryCache>();

            if ( cache == null )
            {
                context.Logger?.LogWarning( "Cache not configured." );
                return next( context, argument );
            }

            var defaultCacheOption = context
                .ServiceProvider
                .GetService<IOptions<PipelineMemoryCacheOptions>>();

            var cacheOption = optionsFunc( argument, defaultCacheOption?.Value ?? new PipelineMemoryCacheOptions() );

            if ( cacheOption?.Key != null )
            {
                return cache.GetOrCreate( cacheOption.Key, entry =>
                {
                    context.Logger?.LogDebug( "Creating cache entry for {Key} not configured", cacheOption.Key );
                    entry.SetOptions( cacheOption );
                    return next( context, argument );
                } ) ?? default;
            }

            context.Logger?.LogError( "Cache entries must have a valid key." );
            context.Exception = new InvalidOperationException( "Cache entries must have a valid key." );
            context.CancelAfter();
            return default;

        } );
    }

    public static IPipelineBuilder<TStart, TNext> PipeCacheAsync<TStart, TOutput, TNext>(
        this IPipelineBuilder<TStart, TOutput> builder,
        FunctionAsync<TOutput, TNext> next,
        Func<TOutput, PipelineMemoryCacheOptions, PipelineMemoryCacheOptions> optionsFunc = null )
    {

        // default to using input as key
        optionsFunc ??= ( output, options ) =>
        {
            options.Key = output;
            return options;
        };


        return builder.PipeAsync( async ( context, argument ) =>
        {
            var cache = context
                .ServiceProvider
                .GetService<IMemoryCache>();

            if ( cache == null )
            {
                context.Logger?.LogWarning( "Cache not configured." );
                return await next( context, argument );
            }

            var defaultCacheOption = context
                .ServiceProvider
                .GetService<IOptions<PipelineMemoryCacheOptions>>();

            var cacheOption = optionsFunc( argument, defaultCacheOption?.Value ?? new PipelineMemoryCacheOptions() );

            if ( cacheOption?.Key != null )
            {
                return await cache.GetOrCreateAsync( cacheOption.Key, entry =>
                {
                    context.Logger?.LogDebug( "Creating cache entry for {Key} not configured", cacheOption.Key );
                    entry.SetOptions( cacheOption );
                    return next( context, argument );
                } ) ?? default;
            }

            context.Logger?.LogError( "Cache entries must have a valid key." );
            context.Exception = new InvalidOperationException( "Cache entries must have a valid key." );
            context.CancelAfter();
            return default;

        } );
    }
}
