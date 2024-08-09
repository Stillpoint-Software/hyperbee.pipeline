using System.Linq.Expressions;
using System.Reflection;
using Hyperbee.Pipeline.Binders.Abstractions;
using Hyperbee.Pipeline.Context;

namespace Hyperbee.Pipeline.Binders;

internal class ReduceBlockBinder<TInput, TOutput, TElement, TNext> : BlockBinder<TInput, TOutput>
{
    private Func<TNext, TNext, TNext> Reducer { get; }

    public ReduceBlockBinder( Func<TNext, TNext, TNext> reducer, Expression<FunctionAsync<TInput, TOutput>> function )
    : base( function, default )
    {
        Reducer = reducer;
    }

    public Expression<FunctionAsync<TInput, TNext>> Bind( Expression<FunctionAsync<TInput, TNext>> next )
    {
        // Get the MethodInfo for the BindImpl method
        var bindImplAsyncMethodInfo = typeof( ReduceBlockBinder<TInput, TOutput, TElement, TNext> )
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
        return Expression.Lambda<FunctionAsync<TInput, TNext>>( callBindImplAsync, paramContext, paramArgument );
    }

    private async Task<TNext> BindImplAsync(
        FunctionAsync<TElement, TNext> next,
        FunctionAsync<TInput, TOutput> pipeline,
        IPipelineContext context,
        TInput argument )
    {
        var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument, pipeline ).ConfigureAwait( false );

        if ( canceled )
            return default;

        var nextArguments = (IEnumerable<TElement>) nextArgument;
        var accumulator = default( TNext );

        // Process each element and apply the reducer
        foreach ( var elementArgument in nextArguments )
        {
            var result = await ProcessBlockAsync( next, context, elementArgument ).ConfigureAwait( false );
            accumulator = Reducer( accumulator, result );
        }

        return accumulator;
    }
}

