using Hyperbee.Pipeline.Binders;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline;

public static class HookBuilder
{
    public static IPipelineStartBuilder<TStart, TOutput> HookAsync<TStart, TOutput>(
        this IPipelineStartBuilder<TStart, TOutput> parent,
        MiddlewareAsync<object, object> functionMiddleware
    )
    {
        return HookBuilder<TStart, TOutput>.HookAsync( parent, functionMiddleware );
    }

    public static IPipelineStartBuilder<TStart, TOutput> HookAsync<TStart, TOutput>(
        this IPipelineStartBuilder<TStart, TOutput> parent,
        IEnumerable<MiddlewareAsync<object, object>> functionMiddleware
    )
    {
        return HookBuilder<TStart, TOutput>.HookAsync( parent, functionMiddleware );
    }
}

internal static class HookBuilder<TStart, TOutput>
{
    public static IPipelineStartBuilder<TStart, TOutput> HookAsync(
        IPipelineStartBuilder<TStart, TOutput> parent,
        MiddlewareAsync<object, object> functionMiddleware
    )
    {
        if ( functionMiddleware == null )
            return parent;

        var (parentFunction, parentMiddleware) = parent.GetPipelineFunction();

        return new PipelineBuilder<TStart, TOutput>
        {
            Function = parentFunction,
            Middleware = new HookBinder<object, object>( parentMiddleware ).Bind( functionMiddleware )
        };
    }

    public static IPipelineStartBuilder<TStart, TOutput> HookAsync(
        IPipelineStartBuilder<TStart, TOutput> parent,
        IEnumerable<MiddlewareAsync<object, object>> functionMiddleware
    )
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
