using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
using System.Reflection;

namespace Hyperbee.Pipeline.Binders;

internal class HookBinder<TInput, TOutput> // explicit Type Args due to <object,object> usage
{
    private Expression<MiddlewareAsync<TInput, TOutput>> Middleware { get; }

    public HookBinder( Expression<MiddlewareAsync<TInput, TOutput>> middleware )
    {
        if ( middleware == null )
        {
            // Create and return the final expression
            var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
            var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );
            Middleware = Expression.Lambda<MiddlewareAsync<TInput, TOutput>>(
                null
                // TODO: empty middleware async ( context, argument, next ) => await next( context, argument ).ConfigureAwait( false )
                , paramContext, paramArgument );
        }

        this.Middleware = middleware;

    }

    public Expression<MiddlewareAsync<TInput, TOutput>> Bind( MiddlewareAsync<TInput, TOutput> middleware )
    {
        // Get the MethodInfo for the BindImpl method
        var bindImplMethodInfo = typeof( HookBinder<TInput, TOutput> )
            .GetMethod( nameof( BindImpl ), BindingFlags.NonPublic )!
            .MakeGenericMethod( typeof( TInput ), typeof( TOutput ) );

        // Create the call expression to BindImpl
        var callBind = Expression.Call(
            bindImplMethodInfo,
            ExpressionBinder.ToExpression( middleware ),
            Middleware
        );

        // Create and return the final expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );
        return Expression.Lambda<MiddlewareAsync<TInput, TOutput>>( callBind, paramContext, paramArgument );
    }

    private MiddlewareAsync<TInput, TOutput> BindImpl( MiddlewareAsync<TInput, TOutput> middleware, MiddlewareAsync<TInput, TOutput> inner )
    {
        return async ( context, argument, function ) =>
            await middleware(
                context,
                argument,
                async ( context1, argument1 ) => await inner( context1, argument1, function ).ConfigureAwait( false )
            ).ConfigureAwait( false );
    }
}
