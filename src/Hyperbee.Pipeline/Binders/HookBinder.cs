using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
using System.Reflection;

namespace Hyperbee.Pipeline.Binders;

internal class HookBinder<TInput1, TOutput1> // explicit Type Args due to <object,object> usage
{
    private Expression<MiddlewareAsync<TInput1, TOutput1>> Middleware { get; }

    public HookBinder( Expression<MiddlewareAsync<TInput1, TOutput1>> middleware )
    {
        if ( middleware == null )
        {
            // Create and return the final expression
            var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
            var paramArgument = Expression.Parameter( typeof( TInput1 ), "argument" );
            Middleware = Expression.Lambda<MiddlewareAsync<TInput1, TOutput1>>(
                null
                // TODO: empty middleware async ( context, argument, next ) => await next( context, argument ).ConfigureAwait( false )
                , paramContext, paramArgument );
        }

        this.Middleware = middleware;

    }

    public Expression<MiddlewareAsync<TInput1, TOutput1>> Bind( MiddlewareAsync<TInput1, TOutput1> middleware )
    {
        // Get the MethodInfo for the BindImpl method
        var bindImplMethodInfo = typeof( HookBinder<TInput1, TOutput1> )
            .GetMethod( nameof( BindImpl ), BindingFlags.NonPublic | BindingFlags.Static )!
            .MakeGenericMethod( typeof( TInput1 ), typeof( TOutput1 ) );

        // Create the call expression to BindImpl
        var callBind = Expression.Call(
            bindImplMethodInfo,
            Expression.Constant( middleware, typeof( MiddlewareAsync<TInput1, TOutput1> ) ),
            Middleware
        );

        // Create and return the final expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput1 ), "argument" );
        return Expression.Lambda<MiddlewareAsync<TInput1, TOutput1>>( callBind, paramContext, paramArgument );
    }

    private static MiddlewareAsync<TInput1, TOutput1> BindImpl( MiddlewareAsync<TInput1, TOutput1> middleware, MiddlewareAsync<TInput1, TOutput1> inner )
    {
        return async ( context, argument, function ) =>
            await middleware(
                context,
                argument,
                async ( context1, argument1 ) => await inner( context1, argument1, function ).ConfigureAwait( false )
            ).ConfigureAwait( false );
    }
}
