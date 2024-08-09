using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline;

public partial interface IPipelineBuilder<TInput, TOutput>
{
    IPipelineBuilder<TInput, TOutput> WrapAsync( MiddlewareAsync<TInput, TOutput> pipelineMiddleware, string name );
    IPipelineBuilder<TInput, TOutput> WrapAsync( MiddlewareAsync<TInput, TOutput> pipelineMiddleware, Action<IPipelineContext> config = null );
    IPipelineBuilder<TInput, TOutput> WrapAsync( IEnumerable<MiddlewareAsync<TInput, TOutput>> pipelineMiddleware, Action<IPipelineContext> config = null );
}

public partial class PipelineBuilder<TInput, TOutput>
{
    public IPipelineBuilder<TInput, TOutput> WrapAsync( MiddlewareAsync<TInput, TOutput> pipelineMiddleware, string name ) => WrapAsync( pipelineMiddleware, config => config.Name = name );

    public IPipelineBuilder<TInput, TOutput> WrapAsync( MiddlewareAsync<TInput, TOutput> pipelineMiddleware, Action<IPipelineContext> config = null )
    {
        if ( pipelineMiddleware == null )
            return this;

        return new PipelineBuilder<TInput, TOutput>
        {
            Function = new WrapBinder<TInput, TOutput>(
                ExpressionBinder.ToExpression( pipelineMiddleware ),
                config ).Bind( Function ),
            Middleware = Middleware
        };
    }

    public IPipelineBuilder<TInput, TOutput> WrapAsync( IEnumerable<MiddlewareAsync<TInput, TOutput>> pipelineMiddleware, Action<IPipelineContext> config = null )
    {
        if ( pipelineMiddleware == null )
            return this;

        var builder = this as IPipelineBuilder<TInput, TOutput>;

        foreach ( var middleware in pipelineMiddleware )
        {
            builder = WrapAsync( middleware, config );
        }

        return builder;
    }
}
