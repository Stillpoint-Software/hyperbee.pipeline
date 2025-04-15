using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Binders;

internal class ForEachBlockBinder<TInput, TOutput, TElement> : BlockBinder<TInput, TOutput>
{
    public ForEachBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function )
        : base( function, default )
    {
    }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( Expression<FunctionAsync<TElement, object>> next )
    {
        // Get the MethodInfo for the helper method
        var bindImplAsyncMethodInfo = typeof( ForEachBlockBinder<TInput, TOutput, TElement> )
            .GetMethod( nameof( BindImplAsync ), BindingFlags.NonPublic | BindingFlags.Instance )!;

        // Create parameters for the lambda expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );

        // Create a call expression to the helper method
        var callBindImplAsync = Expression.Call(
            Expression.Constant( this ),
            bindImplAsyncMethodInfo,
            next,
            Pipeline,
            paramContext,
            paramArgument
        );

        // Create and return the final expression
        return Expression.Lambda<FunctionAsync<TInput, TOutput>>( callBindImplAsync, paramContext, paramArgument );
    }

    private async Task<TOutput> BindImplAsync(
        FunctionAsync<TElement, object> next,
        FunctionAsync<TInput, TOutput> pipeline,
        IPipelineContext context,
        TInput argument )
    {
        var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument, pipeline ).ConfigureAwait( false );

        if ( canceled )
            return default;

        var nextArguments = (IEnumerable<TElement>) nextArgument;

        foreach ( var elementArgument in nextArguments )
        {
            await ProcessBlockAsync( next, context, elementArgument ).ConfigureAwait( false );
        }

        return nextArgument;
    }
}

