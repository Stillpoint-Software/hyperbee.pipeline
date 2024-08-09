using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class WrapBuilder
{
    public static IPipelineBuilder<TInput, TOutput> WrapAsync<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> parent,
        MiddlewareAsync<TInput, TOutput> pipelineMiddleware,
        string name
    )
    {
        return WrapBuilder<TInput, TOutput>.WrapAsync( parent, pipelineMiddleware, config => config.Name = name );
    }

    public static IPipelineBuilder<TInput, TOutput> WrapAsync<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> parent,
        MiddlewareAsync<TInput, TOutput> pipelineMiddleware,
        Action<IPipelineContext> config = null
    )
    {
        return WrapBuilder<TInput, TOutput>.WrapAsync( parent, pipelineMiddleware, config );
    }

    public static IPipelineBuilder<TInput, TOutput> WrapAsync<TInput, TOutput>(
        this IPipelineBuilder<TInput, TOutput> parent,
        IEnumerable<MiddlewareAsync<TInput, TOutput>> pipelineMiddleware,
        Action<IPipelineContext> config = null
    )
    {
        return WrapBuilder<TInput, TOutput>.WrapAsync( parent, pipelineMiddleware, config );
    }
}

internal static class WrapBuilder<TInput, TOutput>
{
    public static IPipelineBuilder<TInput, TOutput> WrapAsync(
        IPipelineBuilder<TInput, TOutput> parent,
        MiddlewareAsync<TInput, TOutput> pipelineMiddleware,
        Action<IPipelineContext> config = null
    )
    {
        if ( pipelineMiddleware == null )
            return parent;

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        return new PipelineBuilder<TInput, TOutput>
        {
            Function = new WrapBinder<TInput, TOutput>( pipelineMiddleware, config ).Bind( parentFunction ),
            Middleware = parentMiddleware
        };
    }

    public static IPipelineBuilder<TInput, TOutput> WrapAsync(
        IPipelineBuilder<TInput, TOutput> parent,
        IEnumerable<MiddlewareAsync<TInput, TOutput>> pipelineMiddleware,
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
