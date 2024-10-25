using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;

using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.ExpressionExtensions;

namespace Hyperbee.Pipeline.Binders;

internal class HookBinder<TInput, TOutput> // explicit Type Args due to <object,object> usage
{
    private Expression<MiddlewareAsync<TInput, TOutput>> Middleware { get; }

    public HookBinder( Expression<MiddlewareAsync<TInput, TOutput>> middleware )
    {
        Middleware = middleware; // Note: no need to create empty middleware just don't execute it.
    }

    // public MiddlewareAsync<TInput, TOutput> Bind( MiddlewareAsync<TInput, TOutput> middleware )
    // {
    //     return async ( context, argument, function ) =>
    //         await middleware(
    //             context,
    //             argument,
    //             async ( context1, argument1 ) => await Middleware( context1, argument1, function ).ConfigureAwait( false )
    //         ).ConfigureAwait( false );
    // }

    public Expression<MiddlewareAsync<TInput, TOutput>> Bind( Expression<MiddlewareAsync<TInput, TOutput>> middleware )
    {
        if ( Middleware == null )
            return middleware;

        var context = Parameter( typeof( IPipelineContext ), "context" );
        var argument = Parameter( typeof( TInput ), "argument" );
        var function = Parameter( typeof( FunctionAsync<TOutput, TInput> ), "function" );

        // inner function
        var ctx = Parameter( typeof( IPipelineContext ), "ctx" );
        var arg = Parameter( typeof( TInput ), "arg" );
        var nextExpression = Lambda<FunctionAsync<TOutput, TInput>>(
        BlockAsync(
                Await( Invoke( Middleware, ctx, arg, function ), configureAwait: false ),
                argument
            ),
            parameters: [ctx, arg]
        );

        return Lambda<MiddlewareAsync<TInput, TOutput>>(
            BlockAsync(
                [context, argument],
                Await( Invoke( middleware, context, argument, nextExpression ), configureAwait: false )
            ),
            parameters: [context, argument, function] );
    }

}
