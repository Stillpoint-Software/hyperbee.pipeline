using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
using System.Reflection;

namespace Hyperbee.Pipeline.Binders;

internal class ReduceBlockBinder<TInput, TOutput, TElement, TNext>
{
    private Expression<FunctionAsync<TInput, TOutput>> Pipeline { get; }
    private Expression<Func<TNext, TNext, TNext>> Reducer { get; }

    public ReduceBlockBinder( Expression<Func<TNext, TNext, TNext>> reducer, Expression<FunctionAsync<TInput, TOutput>> function )
    {
        Reducer = reducer;
        Pipeline = function;
    }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( Expression<FunctionAsync<TInput, TOutput>> next )
    {        
        // Get the MethodInfo for the BindImpl method
        var bindImplMethodInfo = typeof( ReduceBlockBinder<TInput, TOutput, TElement, TNext> )
            .GetMethod( nameof( BindImpl ), BindingFlags.NonPublic | BindingFlags.Static )!
            .MakeGenericMethod( typeof( TInput ), typeof( TOutput ), typeof( TNext ) );

        // Create the call expression to BindImpl
        var callBind = Expression.Call(
            bindImplMethodInfo,
            next,
            Pipeline,
            Expression.Constant( Reducer )
        );

        // Create and return the final expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );
        return Expression.Lambda<FunctionAsync<TInput, TOutput>>( callBind, paramContext, paramArgument );
    }

    private static FunctionAsync<TInput, TNext> BindImpl( FunctionAsync<TElement, TNext> next, FunctionAsync<TInput, TOutput> pipeline, Func<TNext, TNext, TNext> reducer )
    {
        return async ( context, argument ) =>
        {
            var nextArgument = await pipeline( context, argument ).ConfigureAwait( false );
            var nextArguments = (IEnumerable<TElement>) nextArgument;

            var accumulator = default( TNext );

            foreach ( var elementArgument in nextArguments )
            {
                var result = await next( context, elementArgument ).ConfigureAwait( false );
                accumulator = reducer( accumulator, result );
            }

            return accumulator;
        };
    }
}
