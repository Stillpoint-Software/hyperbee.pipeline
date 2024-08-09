using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Binders;

internal class CallStatementBinder<TInput, TOutput> : StatementBinder<TInput, TOutput>
{
    public CallStatementBinder( Expression<FunctionAsync<TInput, TOutput>> function, MiddlewareAsync<object, object> middleware, Action<IPipelineContext> configure )
        : base( function, middleware, configure )
    {
    }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( ProcedureAsync<TOutput> next, MethodInfo method = null )
    {
        var defaultName = (method ?? next.Method).Name;

        // Get the MethodInfo for the helper method
        var bindImplAsyncMethodInfo = typeof( CallStatementBinder<TInput, TOutput> )
            .GetMethod( nameof( BindImplAsync ), BindingFlags.NonPublic | BindingFlags.Instance )!;

        // Create parameters for the lambda expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );

        // Create a call expression to the helper method
        var callBindImplAsync = Expression.Call(
            Expression.Constant( this ),
            bindImplAsyncMethodInfo,
            ExpressionBinder.ToExpression( next ),
            Pipeline,
            paramContext,
            paramArgument,
            Expression.Constant( defaultName )
        );

        // Create and return the final expression
        return Expression.Lambda<FunctionAsync<TInput, TOutput>>( callBindImplAsync, paramContext, paramArgument );
    }

    private async Task<TOutput> BindImplAsync(
        ProcedureAsync<TOutput> next,
        FunctionAsync<TInput, TOutput> pipeline,
        IPipelineContext context,
        TInput argument,
        string defaultName )
    {
        var (nextArgument, canceled) =
            await ProcessPipelineAsync( context, argument, pipeline ).ConfigureAwait( false );

        if ( canceled )
            return default;

        return await ProcessStatementAsync(
            async ( ctx, arg ) =>
            {
                await next( ctx, arg ).ConfigureAwait( false );
                return arg;
            }, context, nextArgument, defaultName ).ConfigureAwait( false );

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

