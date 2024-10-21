using System.Linq.Expressions;
using Hyperbee.Pipeline.Context;

using static System.Linq.Expressions.Expression;
using static Hyperbee.Expressions.AsyncExpression;

namespace Hyperbee.Pipeline.Binders.Abstractions;

internal abstract class BlockBinder<TInput, TOutput> : Binder<TInput, TOutput>
{
    protected BlockBinder( Expression<FunctionAsync<TInput, TOutput>> function, Expression<Action<IPipelineContext>> configure )
        : base( function, configure )
    {
    }

    // Using TArgument instead of TOutput allows more capabilities for special
    // use cases where the next argument is not the same as the output type
    // like ReduceBlockBinder and ForEachBlockBinder

    // protected virtual async Task<TNext> ProcessBlockAsync<TArgument, TNext>( Expression<FunctionAsync<TArgument, TNext>> blockFunction, IPipelineContext context, TArgument nextArgument )
    // {
    //     return await blockFunction( context, nextArgument ).ConfigureAwait( false );
    // }
    protected virtual Expression<Task<TNext>> ProcessBlockAsync<TArgument, TNext>( Expression<FunctionAsync<TArgument, TNext>> blockFunction, ParameterExpression context, Expression nextArgument )
    {
        var body = BlockAsync(
            Await( Invoke( blockFunction, context, nextArgument ), configureAwait: false )
        );

        return Lambda<Task<TNext>>( body );
    }
}
