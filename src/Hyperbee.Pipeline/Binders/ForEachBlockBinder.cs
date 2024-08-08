using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
using System.Reflection;

namespace Hyperbee.Pipeline.Binders;

internal class ForEachBlockBinder<TInput, TOutput, TElement>
{
    private Expression<FunctionAsync<TInput, TOutput>> Pipeline { get; }

    public ForEachBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function )
    {
        Pipeline = function;
    }


    public Expression<FunctionAsync<TInput, TOutput>> Bind( Expression<FunctionAsync<TElement, object>> next )
    {
        // Get the MethodInfo for the BindImpl method
        var bindImplMethodInfo = typeof( ForEachBlockBinder<TInput, TOutput, TElement> )
            .GetMethod( nameof( BindImpl ), BindingFlags.NonPublic | BindingFlags.Static )!
            .MakeGenericMethod( typeof( TInput ), typeof( TOutput ) );

        // Create the call expression to BindImpl
        var callBind = Expression.Call(
            bindImplMethodInfo,
            next,
            Pipeline
        );

        // Create and return the final expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );
        return Expression.Lambda<FunctionAsync<TInput, TOutput>>( callBind, paramContext, paramArgument );
    }

    private static FunctionAsync<TInput, TOutput> BindImpl( FunctionAsync<TElement, object> next, FunctionAsync<TInput, TOutput> pipeline )
    {
        return async ( context, argument ) =>
        {
            var nextArgument = await pipeline( context, argument ).ConfigureAwait( false );
            var nextArguments = (IEnumerable<TElement>) nextArgument;

            foreach ( var elementArgument in nextArguments )
            {
                await next( context, elementArgument ).ConfigureAwait( false );
            }

            return nextArgument;
        };
    }
}
