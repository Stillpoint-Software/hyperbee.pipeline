namespace Hyperbee.Pipeline.Binders;

internal class HookBinder<TInput1, TOutput1> // explicit Type Args due to <object,object> usage
{
    private MiddlewareAsync<TInput1, TOutput1> Middleware { get; }

    public HookBinder( MiddlewareAsync<TInput1, TOutput1> middleware )
    {
        Middleware = middleware ?? (async ( context, argument, next ) => await next( context, argument ).ConfigureAwait( false ));
    }

    public MiddlewareAsync<TInput1, TOutput1> Bind( MiddlewareAsync<TInput1, TOutput1> middleware )
    {
        return async ( context, argument, function ) =>
            await middleware(
                context,
                argument,
                async ( context1, argument1 ) => await Middleware( context1, argument1, function ).ConfigureAwait( false )
            ).ConfigureAwait( false );
    }
}
