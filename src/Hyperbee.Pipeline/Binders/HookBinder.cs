namespace Hyperbee.Pipeline.Binders;

internal class HookBinder<TStart, TOutput> // explicit Type Args due to <object,object> usage
{
    private MiddlewareAsync<TStart, TOutput> Middleware { get; }

    public HookBinder( MiddlewareAsync<TStart, TOutput> middleware )
    {
        Middleware = middleware ?? (async ( context, argument, next ) => await next( context, argument ).ConfigureAwait( false ));
    }

    public MiddlewareAsync<TStart, TOutput> Bind( MiddlewareAsync<TStart, TOutput> middleware )
    {
        return async ( context, argument, function ) =>
            await middleware(
                context,
                argument,
                async ( context1, argument1 ) => await Middleware( context1, argument1, function ).ConfigureAwait( false )
            ).ConfigureAwait( false );
    }
}
