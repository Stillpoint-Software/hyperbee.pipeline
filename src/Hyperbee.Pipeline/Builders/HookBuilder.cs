using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class HookBuilder
{
    public static IPipelineStartBuilder<TInput, TOutput> HookAsync<TInput, TOutput>( this IPipelineStartBuilder<TInput, TOutput> parent, MiddlewareAsync<object, object> functionMiddleware )
    {
        return HookBuilder<TInput, TOutput>.HookAsync( parent, functionMiddleware );
    }

    public static IPipelineStartBuilder<TInput, TOutput> HookAsync<TInput, TOutput>( this IPipelineStartBuilder<TInput, TOutput> parent, IEnumerable<MiddlewareAsync<object, object>> functionMiddleware )
    {
        return HookBuilder<TInput, TOutput>.HookAsync( parent, functionMiddleware );
    }
}

public static class HookBuilder<TInput, TOutput> 
{
    public static IPipelineStartBuilder<TInput, TOutput> HookAsync( IPipelineStartBuilder<TInput, TOutput> parent, MiddlewareAsync<object, object> functionMiddleware )
    {
        if ( functionMiddleware == null )
            return parent;

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        return new PipelineBuilder<TInput, TOutput> 
        { 
            Function = parentFunction, 
            Middleware = new HookBinder<object, object>( parentMiddleware ).Bind( functionMiddleware ) 
        };
    }

    public static IPipelineStartBuilder<TInput, TOutput> HookAsync( IPipelineStartBuilder<TInput, TOutput> parent, IEnumerable<MiddlewareAsync<object, object>> functionMiddleware )
    {
        if ( functionMiddleware == null )
            return parent;

        var builder = parent;

        foreach ( var middleware in functionMiddleware )
        {
            builder = HookAsync( parent, middleware );
        }

        return builder;
    }
}
