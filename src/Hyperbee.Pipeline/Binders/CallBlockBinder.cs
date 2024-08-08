using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
using System.Reflection;

using Hyperbee.Pipeline.Binders.Abstractions;

namespace Hyperbee.Pipeline.Binders;

internal class CallBlockBinder<TInput, TOutput> : ConditionalBlockBinder<TInput, TOutput>
{
    public CallBlockBinder( Expression<FunctionAsync<TInput, TOutput>> function )
        : base( null, function, default )
    {
    }

    public CallBlockBinder( Expression<Function<TOutput, bool>> condition, Expression<FunctionAsync<TInput, TOutput>> function )
        : base( condition, function, default )
    {
    }

    public Expression<FunctionAsync<TInput, TOutput>> Bind( FunctionAsync<TOutput, object> next )
    {
        // Get the MethodInfo for the BindImpl method
        var bindImplMethodInfo = typeof( CallBlockBinder<TInput, TOutput> )
            .GetMethod( nameof( BindImpl ), BindingFlags.NonPublic )!
            .MakeGenericMethod( typeof( TInput ), typeof( TOutput ) );

        // Create the call expression to BindImpl
        var callBind = Expression.Call(
            bindImplMethodInfo,
            ExpressionBinder.ToExpression( next ),
            Pipeline,
            Condition
        );

        // Create and return the final expression
        var paramContext = Expression.Parameter( typeof( IPipelineContext ), "context" );
        var paramArgument = Expression.Parameter( typeof( TInput ), "argument" );
        return Expression.Lambda<FunctionAsync<TInput, TOutput>>( callBind, paramContext, paramArgument );
    }

    private FunctionAsync<TInput, TOutput> BindImpl( FunctionAsync<TOutput, object> next, FunctionAsync<TInput, TOutput> pipeline, Function<TOutput, bool> condition )
    {
        return async ( context, argument ) =>
        {
            var (nextArgument, canceled) = await ProcessPipelineAsync( context, argument, pipeline ).ConfigureAwait( false );

            if ( canceled )
                return default;

            await ProcessBlockAsync( next, context, nextArgument ).ConfigureAwait( false );
            return nextArgument;
        };
    }
}
