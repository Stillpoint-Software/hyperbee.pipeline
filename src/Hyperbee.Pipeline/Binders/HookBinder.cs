namespace Hyperbee.Pipeline.Binders;

internal class HookBinder<TInput, TOutput> // explicit Type Args due to <object,object> usage
{
    private MiddlewareAsync<TInput, TOutput> Middleware { get; }

    public HookBinder( MiddlewareAsync<TInput, TOutput> middleware )
    {
        Middleware = middleware ?? (async ( context, argument, next ) => await next( context, argument ).ConfigureAwait( false ));
    }

    public MiddlewareAsync<TInput, TOutput> Bind( MiddlewareAsync<TInput, TOutput> middleware )
    {
        return async ( context, argument, function ) =>
            await middleware(
                context,
                argument,
                async ( context1, argument1 ) => await Middleware( context1, argument1, function ).ConfigureAwait( false )
            ).ConfigureAwait( false );
    }
}
