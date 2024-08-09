using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Binders;

internal class PipeStatementBinder<TInput, TOutput> : StatementBinder<TInput, TOutput>
{
    public PipeStatementBinder( Expression<FunctionAsync<TInput, TOutput>> function,
        MiddlewareAsync<object, object> middleware, Action<IPipelineContext> configure )
        : base( function, middleware, configure )
    {
    }

    public Expression<FunctionAsync<TInput, TNext>> Bind<TNext>( FunctionAsync<TOutput, TNext> next,
        MethodInfo method = null )
    {
        var defaultName = (method ?? next.Method).Name;

        // Get the MethodInfo for the helper method
        var bindImplAsyncMethodInfo = typeof( PipeStatementBinder<TInput, TOutput> )
            .GetMethod( nameof( BindImplAsync ), BindingFlags.NonPublic | BindingFlags.Instance )!
            .MakeGenericMethod( typeof( TNext ) );

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
        return Expression.Lambda<FunctionAsync<TInput, TNext>>( callBindImplAsync, paramContext, paramArgument );
    }

    private async Task<TNext> BindImplAsync<TNext>(
        FunctionAsync<TOutput, TNext> next,
        FunctionAsync<TInput, TOutput> pipeline,
        IPipelineContext context,
        TInput argument,
        string defaultName )
    {
        var (nextArgument, canceled) =
            await ProcessPipelineAsync( context, argument, pipeline ).ConfigureAwait( false );

        if ( canceled )
            return default;

        return await ProcessStatementAsync( next, context, nextArgument, defaultName ).ConfigureAwait( false );
    }
}
