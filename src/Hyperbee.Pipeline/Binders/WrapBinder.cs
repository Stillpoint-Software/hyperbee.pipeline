using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders;

internal class WrapBinder<TStart, TOutput>
{
    private MiddlewareAsync<TStart, TOutput> Middleware { get; }
    private Action<IPipelineContext> Configure { get; }

    public WrapBinder( MiddlewareAsync<TStart, TOutput> middleware, Action<IPipelineContext> configure )
    {
        Middleware = middleware;
        Configure = configure;
    }

    public FunctionAsync<TStart, TOutput> Bind( FunctionAsync<TStart, TOutput> next )
    {
        var defaultName = next.Method.Name;

        return async ( context, argument ) =>
        {
            var contextControl = (IPipelineContextControl) context;

            using var _ = contextControl.CreateFrame( context, Configure, defaultName );

            return await Middleware(
                context,
                argument,
                async ( context1, argument1 ) => await next( context1, argument1 ).ConfigureAwait( false )
            ).ConfigureAwait( false );
        };
    }
}
