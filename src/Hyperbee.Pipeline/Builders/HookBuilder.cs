using Hyperbee.Pipeline.Binders;

namespace Hyperbee.Pipeline;

public partial interface IPipelineStartBuilder<TInput, TOutput>
{
    IPipelineStartBuilder<TInput, TOutput> HookAsync( MiddlewareAsync<object, object> functionMiddleware );
    IPipelineStartBuilder<TInput, TOutput> HookAsync( IEnumerable<MiddlewareAsync<object, object>> functionMiddleware );
}

public partial class PipelineBuilder<TInput, TOutput>
{
    public IPipelineStartBuilder<TInput, TOutput> HookAsync( MiddlewareAsync<object, object> functionMiddleware )
    {
        if ( functionMiddleware == null )
            return this;

        return new PipelineBuilder<TInput, TOutput>
        {
            Function = Function,
            Middleware = new HookBinder<object, object>( Middleware ).Bind( functionMiddleware )
        };
    }

    public IPipelineStartBuilder<TInput, TOutput> HookAsync( IEnumerable<MiddlewareAsync<object, object>> functionMiddleware )
    {
        if ( functionMiddleware == null )
            return this;

        var builder = this as IPipelineStartBuilder<TInput, TOutput>;

        foreach ( var middleware in functionMiddleware )
        {
            builder = HookAsync( middleware );
        }

        return builder;
    }
}
