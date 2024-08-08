using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Extensions.Implementation;

namespace Hyperbee.Pipeline.Binders;

internal class WrapBinder<TInput, TOutput>
{
    private Expression<MiddlewareAsync<TInput, TOutput>> Middleware { get; }
    private Expression<Action<IPipelineContext>> Configure { get; }

    public WrapBinder( Expression<MiddlewareAsync<TInput, TOutput>> middleware, Expression<Action<IPipelineContext>> configure )
    {
        Middleware = middleware;
        Configure = configure;
    }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( FunctionAsync<TInput, TOutput> next )
    {
        // Get the MethodInfo for the BindImpl method
        var bindImplMethodInfo = typeof( WrapBinder<TInput, TOutput> )
            .GetMethod( nameof( BindImpl ), BindingFlags.NonPublic | BindingFlags.Static )!
            .MakeGenericMethod( typeof( TInput ), typeof( TOutput ) );

        // Create the call expression to BindImpl
        var callBind = Expression.Call(
            bindImplMethodInfo,
            ExpressionBinder.ToExpression( next ),
            Middleware,
            Configure
        );

        // Create and return the final expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );
        return Expression.Lambda<FunctionAsync<TInput, TOutput>>( callBind, paramContext, paramArgument );
    }

    private static FunctionAsync<TInput, TOutput> BindImpl( FunctionAsync<TInput, TOutput> next, MiddlewareAsync<TInput,TOutput> middleware, Action<IPipelineContext> configure )
    {
        var defaultName = next.Method.Name;

        return async ( context, argument ) =>
        {
            var contextControl = (IPipelineContextControl) context;

            using var _ = contextControl.CreateFrame( context, configure, defaultName );

            return await middleware(
                context,
                argument,
                async ( context1, argument1 ) => await next( context1, argument1 ).ConfigureAwait( false )
            ).ConfigureAwait( false );
        };
    }
}
