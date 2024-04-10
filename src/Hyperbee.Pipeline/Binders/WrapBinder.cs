using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders;

internal class WrapBinder<TInput, TOutput>
{
    private MiddlewareAsync<TInput, TOutput> Middleware { get; }
    private Action<IPipelineContext> Configure { get; }

    public WrapBinder( MiddlewareAsync<TInput, TOutput> middleware, Action<IPipelineContext> configure )
    {
        Middleware = middleware;
        Configure = configure;
    }

    public FunctionAsync<TInput, TOutput> Bind( FunctionAsync<TInput, TOutput> next )
    {
        var defaultName = next.Method.Name;

        return async ( context, argument ) =>
        {
            var contextControl = (IPipelineContextControl) context;

            using ( contextControl.CreateFrame( context, Configure, defaultName ) )
            {
                return await Middleware(
                    context,
                    argument,
                    async ( context1, argument1 ) => await next( context1, argument1 ).ConfigureAwait( false )
                ).ConfigureAwait( false );
            }
        };
    }
}
