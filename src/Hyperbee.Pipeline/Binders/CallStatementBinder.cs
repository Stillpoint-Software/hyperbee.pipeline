using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Binders;

internal class CallStatementBinder<TInput, TOutput> : StatementBinder<TInput, TOutput>
{
    public CallStatementBinder( Expression<FunctionAsync<TInput, TOutput>> function, Expression<MiddlewareAsync<object, object>> middleware, Expression<Action<IPipelineContext>> configure )
        : base( function, middleware, configure )
    {
    }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( ProcedureAsync<TOutput> next, MethodInfo method = null )
    {
        // Get the MethodInfo for the BindImpl method
        var bindImplMethodInfo = typeof( CallStatementBinder<TInput, TOutput> )
            .GetMethod( nameof( BindImpl ), BindingFlags.NonPublic )!
            .MakeGenericMethod( typeof( TInput ), typeof( TOutput ) );

        // Create the call expression to BindImpl
        var callBind = Expression.Call(
            bindImplMethodInfo,
            ExpressionBinder.ToExpression( next ),
            Pipeline,
            Configure,
            Expression.Constant( method, typeof( MethodInfo ) )
        );

        // Create and return the final expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );
        return Expression.Lambda<FunctionAsync<TInput, TOutput>>(callBind, paramContext, paramArgument);
    }

    public FunctionAsync<TInput, TOutput> BindImpl( 
        ProcedureAsync<TOutput> next, 
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

            return await ProcessStatementAsync(
                async ( ctx, arg ) =>
                {
                    await next( ctx, arg ).ConfigureAwait( false );
                    return arg;
                }, context, nextArgument, defaultName ).ConfigureAwait( false );
        };
    }

    /*
    private async Task<TOutput> Next( ProcedureAsync<TOutput> next, MiddlewareAsync<object, object> middleware, IPipelineContext context, TOutput nextArgument )
    {
        if ( Middleware == null )
        {
            await next( context, nextArgument ).ConfigureAwait( false );
            return nextArgument;
        }

        await middleware(
            context,
            nextArgument,
            async ( context1, argument1 ) =>
            {
                await next( context1, (TOutput) argument1 ).ConfigureAwait( false );
                return nextArgument;
            }
        ).ConfigureAwait( false );

        return nextArgument;
    }*/

}

