namespace Hyperbee.Pipeline;

/// <summary>
/// Provides extension methods for applying middleware from an <see cref="IPipelineMiddlewareProvider"/>
/// to a pipeline builder.
/// </summary>
public static class MiddlewareProviderBuilder
{
    /// <summary>
    /// Applies hook middleware from the provider at the start of the pipeline.
    /// </summary>
    /// <typeparam name="TStart">The input type of the pipeline.</typeparam>
    /// <typeparam name="TOutput">The output type of the pipeline.</typeparam>
    /// <param name="builder">The pipeline start builder.</param>
    /// <param name="provider">The middleware provider supplying hooks.</param>
    /// <returns>The pipeline start builder with hooks applied.</returns>
    public static IPipelineStartBuilder<TStart, TOutput> UseHooks<TStart, TOutput>(
        this IPipelineStartBuilder<TStart, TOutput> builder,
        IPipelineMiddlewareProvider provider
    )
    {
        if ( provider == null || provider.Hooks == null || !provider.Hooks.Any() )
            return builder;

        return builder.HookAsync( provider.Hooks );
    }

    /// <summary>
    /// Applies wrap middleware from the provider around the pipeline.
    /// </summary>
    /// <typeparam name="TStart">The input type of the pipeline.</typeparam>
    /// <typeparam name="TOutput">The output type of the pipeline.</typeparam>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="provider">The middleware provider supplying wraps.</param>
    /// <returns>The pipeline builder with wraps applied.</returns>
    public static IPipelineBuilder<TStart, TOutput> UseWraps<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> builder,
        IPipelineMiddlewareProvider provider
    )
    {
        if ( provider == null || provider.Wraps == null || !provider.Wraps.Any() )
            return builder;

        foreach ( var wrap in provider.Wraps )
        {
            var capturedWrap = wrap;
            builder = builder.WrapAsync(
                async ( context, argument, next ) =>
                {
                    var result = await capturedWrap(
                        context,
                        argument!,
                        async ( ctx, arg ) => (object) await next( ctx, (TStart) arg )!
                    ).ConfigureAwait( false );

                    return (TOutput) result;
                }
            );
        }

        return builder;
    }
}
