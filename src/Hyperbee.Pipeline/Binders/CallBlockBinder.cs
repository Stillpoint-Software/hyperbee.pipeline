using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
using System.Reflection;

namespace Hyperbee.Pipeline.Binders;

internal class CallBlockBinder<TInput, TOutput>
{
    private Expression<FunctionAsync<TInput, TOutput>> Pipeline { get; }
    private Expression<Function<TOutput, bool>> Condition { get; }

    public CallBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function )
        : this( null, function )
    {
    }

    public CallBlockBinder( Expression<Function<TOutput, bool>> condition, Expression<FunctionAsync<TInput, TOutput>> function )
    {
        Condition = condition;
        Pipeline = function;
    }
    public Expression<FunctionAsync<TInput, TOutput>> Bind( Expression<FunctionAsync<TOutput, object>> next )
    {
        // Get the MethodInfo for the BindImpl method
        var bindImplMethodInfo = typeof( CallBlockBinder<TInput, TOutput> )
            .GetMethod( nameof( BindImpl ), BindingFlags.NonPublic | BindingFlags.Static )!
            .MakeGenericMethod( typeof( TInput ), typeof( TOutput ) );

        // Create the call expression to BindImpl
        var callBind = Expression.Call(
            bindImplMethodInfo,
            next,
            Pipeline,
            Condition
        );

        // Create and return the final expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );
        return Expression.Lambda<FunctionAsync<TInput, TOutput>>( callBind, paramContext, paramArgument );
    }

    private static FunctionAsync<TInput, TOutput> BindImpl( FunctionAsync<TOutput, object> next, FunctionAsync<TInput, TOutput> pipeline, Function<TOutput, bool> condition )
    {
        return async ( context, argument ) =>
        {
            var nextArgument = await pipeline( context, argument ).ConfigureAwait( false );

            if ( condition == null || condition( context, nextArgument ) )
                await next( context, nextArgument ).ConfigureAwait( false );

            return nextArgument;
        };
    }
}
