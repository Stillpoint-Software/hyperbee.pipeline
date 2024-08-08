using Hyperbee.Pipeline.Binders.Abstractions;

using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
using System.Reflection;

namespace Hyperbee.Pipeline.Binders;

internal class ReduceBlockBinder<TInput, TOutput, TElement, TNext> : BlockBinder<TInput, TOutput>
{
    private Func<TNext, TNext, TNext> Reducer { get; }

    public ReduceBlockBinder( Func<TNext, TNext, TNext> reducer, Expression<FunctionAsync<TInput, TOutput>> function )
    : base( function, default )
    {
        Reducer = reducer;
    }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( FunctionAsync<TInput, TOutput> next )
    {        
        // Get the MethodInfo for the BindImpl method
        var bindImplMethodInfo = typeof( ReduceBlockBinder<TInput, TOutput, TElement, TNext> )
            .GetMethod( nameof( BindImpl ), BindingFlags.NonPublic )!
            .MakeGenericMethod( typeof( TInput ), typeof( TOutput ), typeof( TNext ) );

        // Create the call expression to BindImpl
        var callBind = Expression.Call(
            bindImplMethodInfo,
            ExpressionBinder.ToExpression( next ),
            Pipeline
        );

        // Create and return the final expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );
        return Expression.Lambda<FunctionAsync<TInput, TOutput>>( callBind, paramContext, paramArgument );
    }

    private FunctionAsync<TInput, TNext> BindImpl( FunctionAsync<TElement, TNext> next, FunctionAsync<TInput, TOutput> pipeline )
    {
        return async ( context, argument ) =>
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
        };
    }
}

