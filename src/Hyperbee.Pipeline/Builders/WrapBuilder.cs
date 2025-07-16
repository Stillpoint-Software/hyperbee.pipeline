using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class WrapBuilder
{
    public static IPipelineBuilder<TStart, TOutput> WrapAsync<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        MiddlewareAsync<TStart, TOutput> pipelineMiddleware,
        string name
    )
    {
        return WrapBuilder<TStart, TOutput>.WrapAsync( parent, pipelineMiddleware, config => config.Name = name );
    }

    public static IPipelineBuilder<TStart, TOutput> WrapAsync<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        MiddlewareAsync<TStart, TOutput> pipelineMiddleware,
        Action<IPipelineContext> config = null
    )
    {
        return WrapBuilder<TStart, TOutput>.WrapAsync( parent, pipelineMiddleware, config );
    }

    public static IPipelineBuilder<TStart, TOutput> WrapAsync<TStart, TOutput>(
        this IPipelineBuilder<TStart, TOutput> parent,
        IEnumerable<MiddlewareAsync<TStart, TOutput>> pipelineMiddleware,
        Action<IPipelineContext> config = null
    )
    {
        return WrapBuilder<TStart, TOutput>.WrapAsync( parent, pipelineMiddleware, config );
    }
}

internal static class WrapBuilder<TStart, TOutput>
{
    public static IPipelineBuilder<TStart, TOutput> WrapAsync(
        IPipelineBuilder<TStart, TOutput> parent,
        MiddlewareAsync<TStart, TOutput> pipelineMiddleware,
        Action<IPipelineContext> config = null
    )
    {
        if ( pipelineMiddleware == null )
            return parent;

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        return new PipelineBuilder<TStart, TOutput>
        {
            Function = new WrapBinder<TStart, TOutput>( pipelineMiddleware, config ).Bind( parentFunction ),
            Middleware = parentMiddleware
        };
    }

    public static IPipelineBuilder<TStart, TOutput> WrapAsync(
        IPipelineBuilder<TStart, TOutput> parent,
        IEnumerable<MiddlewareAsync<TStart, TOutput>> pipelineMiddleware,
        Action<IPipelineContext> config = null
    )
    {
        if ( pipelineMiddleware == null )
            return parent;

        var builder = parent;

        foreach ( var middleware in pipelineMiddleware )
        {
            builder = WrapAsync( parent, middleware, config );
        }

        return builder;
    }
}
