using System.Linq.Expressions;
using System.Reflection;
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

    public Expression<FunctionAsync<TInput, TOutput>> Bind( Expression<FunctionAsync<TInput, TOutput>> next )
    {
        // Get the MethodInfo for the helper method
        var bindImplAsyncMethodInfo = typeof( WrapBinder<TInput, TOutput> )
            .GetMethod( nameof( BindImplAsync ), BindingFlags.NonPublic | BindingFlags.Instance )!;

        // Create parameters for the lambda expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );

        // Create a call expression to the helper method
        var callBindImplAsync = Expression.Call(
            Expression.Constant( this ),
            bindImplAsyncMethodInfo,
            next,
            ExpressionBinder.ToExpression( Middleware ),
            paramContext,
            paramArgument
        );

        // Create and return the final expression
        return Expression.Lambda<FunctionAsync<TInput, TOutput>>( callBindImplAsync, paramContext, paramArgument );
    }

    private async Task<TOutput> BindImplAsync(
        FunctionAsync<TInput, TOutput> next,
        MiddlewareAsync<TInput, TOutput> middleware,
        IPipelineContext context,
        TInput argument )
    {
        var defaultName = next.Method.Name;

        var contextControl = (IPipelineContextControl) context;

        using var _ = contextControl.CreateFrame( context, Configure, defaultName );

        return await middleware(
            context,
            argument,
            async ( context1, argument1 ) => await next( context1, argument1 ).ConfigureAwait( false )
        ).ConfigureAwait( false );
    }
}
