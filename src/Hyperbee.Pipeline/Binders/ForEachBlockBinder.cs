using Hyperbee.Pipeline.Binders.Abstractions;

using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;
using System.Reflection;

namespace Hyperbee.Pipeline.Binders;

internal class ForEachBlockBinder<TInput, TOutput, TElement> : BlockBinder<TInput, TOutput>
{
    public ForEachBlockBinder( FunctionAsync<TInput, TOutput> function )
        : base( function, default )
    {
    }


    public Expression<FunctionAsync<TInput, TOutput>> Bind( FunctionAsync<TElement, object> next )
    {
        // Get the MethodInfo for the BindImpl method
        var bindImplMethodInfo = typeof( ForEachBlockBinder<TInput, TOutput, TElement> )
            .GetMethod( nameof( BindImpl ), BindingFlags.NonPublic )!
            .MakeGenericMethod( typeof( TInput ), typeof( TOutput ) );

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

    private FunctionAsync<TInput, TOutput> BindImpl( FunctionAsync<TElement, object> next, FunctionAsync<TInput, TOutput> pipeline )
    {
        return async ( context, argument ) =>
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
        };
    }
}

