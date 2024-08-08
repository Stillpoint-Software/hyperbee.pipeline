using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Binders;

internal class PipeStatementBinder<TInput, TOutput> : StatementBinder<TInput, TOutput>
{
    public PipeStatementBinder( Expression<FunctionAsync<TInput, TOutput>> function, Expression<MiddlewareAsync<object, object>> middleware, Expression<Action<IPipelineContext>> configure )
        : base( function, middleware, configure )
    {
    }

    public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>( FunctionAsync<TOutput, TNext> next, MethodInfo method = null )
    {
        // Get the MethodInfo for the BindImpl method
        var bindImplMethodInfo = typeof( PipeStatementBinder<TInput, TOutput> )
            .GetMethod( nameof( BindImpl ), BindingFlags.NonPublic )!
            .MakeGenericMethod( typeof( TInput ), typeof( TOutput ), typeof( TNext ) );

        // Create the call expression to BindImpl
        var callBind = Expression.Call(
            bindImplMethodInfo,
            ExpressionBinder.ToExpression( next ),
            Pipeline,
            Middleware,
            Configure,
            Expression.Constant( method, typeof( MethodInfo ) )
        );

        // Create and return the final expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );
        return Expression.Lambda<FunctionAsync<TInput, TNext>>( callBind, paramContext, paramArgument );

    }

    private FunctionAsync<TInput, TNext> BindImpl<TNext>( 
        FunctionAsync<TOutput, TNext> next,
        FunctionAsync<TInput, TOutput> pipeline,
        MiddlewareAsync<object, object> middleware,
        Action<IPipelineContext> configure,
        MethodInfo method = null )
    {
        var defaultName = (method ?? next.Method).Name;

        return async ( context, argument ) =>
        {
            var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument, pipeline ).ConfigureAwait( false );

            if ( canceled )
                return default;

            return await ProcessStatementAsync( next, context, nextArgument, defaultName ).ConfigureAwait( false );
        };
    }

    /*
    private static async Task<TNext> Next<TNext>( 
        FunctionAsync<TOutput, TNext> next,
        MiddlewareAsync<object, object> middleware,
        IPipelineContext context, 
        TOutput nextArgument )
    {
        if ( middleware == null )
            return await next( context, nextArgument ).ConfigureAwait( false );

        return (TNext) await middleware(
            context,
            nextArgument,
            async ( context1, argument1 ) => await next( context1, (TOutput) argument1 ).ConfigureAwait( false )
        ).ConfigureAwait( false );
    }*/
}
